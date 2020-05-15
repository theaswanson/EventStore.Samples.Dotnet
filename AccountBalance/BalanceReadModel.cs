using System;
using System.IO;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace AccountBalance
{
    /// <summary>
    /// An in-memory read model that persists a checkpoint and current state on disk 
    /// for recovery between runs. 
    /// If the checkpoint file does no exist we replay from the beginning 
    /// </summary>
    public class BalanceReadModel
    {
        public int? Checkpoint { get { return _checkpoint.ID; } }
        readonly string _streamName;
        readonly string _localFile;
        readonly ConsoleView _view;
        Checkpoint _checkpoint;

        public BalanceReadModel(ConsoleView view, string streamName, string localFile)
        {
            _view = view;
            _streamName = streamName;
            _localFile = localFile;
            _checkpoint = new Checkpoint(localFile);

            _view.Total = _checkpoint.Value;
            Subscribe();
        }

        private void Subscribe()
        {
            EventStoreLoader.Connection.SubscribeToStreamFrom(_streamName, Checkpoint, false, GotEvent, subscriptionDropped: Dropped);
        }

        private void Dropped(EventStoreCatchUpSubscription sub, SubscriptionDropReason reason, Exception ex)
        {
            //Reconnect if we drop
            //TODO: check the reason and handle it appropriately
            _view.ErrorMsg = "Subscription Dropped, press Enter to reconnect";
            Subscribe();
        }

        private void GotEvent(EventStoreCatchUpSubscription sub, ResolvedEvent evt)
        {
            try
            {
                //create local copies of state variables
                var total = _checkpoint.Value;
                var checkpoint = evt.Event.EventNumber;

                var amount = (string)JObject.Parse(Encoding.UTF8.GetString(evt.Event.Data))["amount"];
                switch (evt.Event.EventType.ToUpperInvariant())
                {
                    case "CREDIT":
                        total += int.Parse(amount);
                        break;
                    case "DEBIT":
                        total -= int.Parse(amount);
                        break;
                    default:
                        throw new Exception("Unknown Event Type");
                }
                _checkpoint.Save(checkpoint, total);
            }
            catch (Exception ex)
            {
                _view.ErrorMsg = "Event Exception: " + ex.Message;
            }
            //repaint screen
            _view.Total = _checkpoint.Value;
        }
    }
}