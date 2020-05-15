using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace AccountBalance
{
    public class Controller
    {
        private readonly ConsoleView _view;
        private readonly BalanceReadModel _rm;
        private readonly string _streamName;
        private readonly string _localFile;

        public Controller(ConsoleView view, BalanceReadModel rm, string streamName, string localFile)
        {
            _view = view;
            _rm = rm;
            _streamName = streamName;
            _localFile = localFile;
        }
        
        public void StartCommandLoop()
        {
            do
            {
                var cmd = GetCommand();
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    _view.Redraw();
                    continue;
                }

                if (cmd.Equals("clean", StringComparison.OrdinalIgnoreCase))
                {
                    Clean();
                    break;
                }
                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Exit();
                    break;
                }
                if (cmd.Equals("repeat", StringComparison.OrdinalIgnoreCase))
                {
                    RepeatLast();
                    continue;
                }
                if (cmd.Equals("undo", StringComparison.OrdinalIgnoreCase))
                {
                    ReverseLastTransaction();
                    continue;
                }
                if (cmd.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    ListEvents();
                    continue;
                }
                if (cmd.Equals("rlist", StringComparison.OrdinalIgnoreCase))
                {
                    ListEvents(true);
                    continue;
                }

                var tokens = cmd.Split(' ');
                if (tokens.Length != 2)
                {
                    SetErrorMessage("Unknown command or Invalid number of parameters.");
                    continue;
                }
                int parameter;
                if (!int.TryParse(tokens[1], out parameter))
                {
                    SetErrorMessage("Command parameter not type int.");
                    continue;
                }
                string command = tokens[0];
                switch (command.ToUpperInvariant())
                {
                    case "CREDIT":
                        AddEvent(parameter, "CREDIT");
                        break;
                    case "DEBIT":
                        AddEvent(parameter, "DEBIT");
                        break;
                    case "REPEAT":
                        RepeatEvent(parameter);
                        break;
                    default:
                        SetErrorMessage("Unknown Command");
                        break;
                }
            } while (true);
        }

        private static void Exit()
        {
            Console.WriteLine("Disconnecting EventStore");
            EventStoreLoader.TeardownEventStore();
        }

        private void Clean()
        {
            Console.WriteLine("Shutting down EventStore and Cleaning data");
            EventStoreLoader.TeardownEventStore(false, true);
            File.Delete(_localFile);
        }

        private static string GetCommand()
        {
            return Console.ReadLine();
        }

        private void SetErrorMessage(string message)
        {
            _view.ErrorMsg = message;
        }

        private void AddEvent(int parameter, string type)
        {
            EventStoreLoader.Connection.AppendToStreamAsync(
                                        _streamName,
                                        _rm.Checkpoint ?? ExpectedVersion.EmptyStream,
                                        new EventData(
                                            Guid.NewGuid(),
                                            type,
                                            true,
                                            Encoding.UTF8.GetBytes("{amount:" + parameter.ToString() + "}"),
                                            new byte[] { }
                                            )
                                        );
        }

        private void ReverseLastTransaction()
        {
            var slice = GetLastEventSlice();

            if (!slice.Events.Any() || !_rm.Checkpoint.HasValue)
            {
                SetErrorMessage("Event not found to undo");
                return;
            }

            var evt = GetLastEvent(slice);
            var amount = int.Parse((string)JObject.Parse(Encoding.UTF8.GetString(evt.Data))["amount"]);
            var reversedAmount = amount * -1;

            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                _rm.Checkpoint.Value,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    Encoding.UTF8.GetBytes("{amount:" + reversedAmount + "}"),
                    evt.Metadata));

        }

        private static RecordedEvent GetLastEvent(StreamEventsSlice slice)
        {
            return slice.Events[0].Event;
        }

        private StreamEventsSlice GetLastEventSlice()
        {
            return EventStoreLoader.Connection.ReadStreamEventsBackwardAsync(
                            _streamName,
                            StreamPosition.End,
                            1,
                            false).Result;
        }

        private void RepeatLast()
        {
            var slice = GetLastEventSlice();

            if (!slice.Events.Any())
            {
                SetErrorMessage("Event not found to repeat");
                return;
            }

            var evt = GetLastEvent(slice);
            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                evt.EventNumber,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    evt.Data,
                    evt.Metadata));
        }

        private void RepeatEvent(int position)
        {
            var result = EventStoreLoader.Connection.ReadEventAsync(
                _streamName,
                position,
                false).Result;

            if (result.Status != EventReadStatus.Success || result.Event == null)
            {
                SetErrorMessage("Event not found to repeat");
                return;
            }

            var evt = result.Event.Value.Event;
            EventStoreLoader.Connection.AppendToStreamAsync(
                evt.EventStreamId,
                ExpectedVersion.Any,
                new EventData(
                    Guid.NewGuid(),
                    evt.EventType,
                    evt.IsJson,
                    evt.Data,
                    evt.Metadata));
        }

        private void ListEvents(bool reversed = false)
        {
            var streamEvents = GetEvents(reversed);
            _view.EventList = streamEvents;
        }

        private List<ResolvedEvent> GetEvents(bool reversed)
        {
            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            var nextSliceStart = reversed ? StreamPosition.End : StreamPosition.Start;
            do
            {
                currentSlice =
                    reversed ?
                        EventStoreLoader.Connection.ReadStreamEventsBackwardAsync(
                            _streamName,
                            nextSliceStart,
                            20,
                            false).Result
                        :
                        EventStoreLoader.Connection.ReadStreamEventsForwardAsync(
                            _streamName,
                            nextSliceStart,
                            20,
                            false).Result;
                nextSliceStart = currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);

            return streamEvents;
        }
    }
}