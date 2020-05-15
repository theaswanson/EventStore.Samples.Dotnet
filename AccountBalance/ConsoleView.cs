using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace AccountBalance
{
    public class ConsoleView
    {
        //hack reactive bindings 
        private int _credits;
        public int Credits { get { return _credits; } set { _credits = value; Redraw(); } }

        private int _debits;
        public int Debits { get { return _debits; } set { _debits = value; Redraw(); } }

        private int _transactions;
        public int Transactions { get { return _transactions; } set { _transactions = value; Redraw(); } }

        private int _total;
        public int Total { get { return _total; } set { _total = value; Redraw(); } }

        private string _errorMsg;
        public string ErrorMsg { get { return _errorMsg; } set { _errorMsg = value; Error(); } }

        private List<ResolvedEvent> _eventList;
        public List<ResolvedEvent> EventList { get { return _eventList; } set { _eventList = value; ListEvents(); } }

        public void Redraw()
        {
            Console.Clear();
            Console.WriteLine("Available Commands:");
            Console.WriteLine("\t credit #");
            Console.WriteLine("\t debit #");
            Console.WriteLine("\t repeat");
            Console.WriteLine("\t repeat {event #}");
            Console.WriteLine("\t list");
            Console.WriteLine("\t rlist");
            Console.WriteLine("\t exit");
            Console.WriteLine("\t clean");
            Console.WriteLine("\t undo");
            Console.WriteLine("Session Stats: Credits - {0}, Debits - {1}, Transactions - {2}", _credits, _debits, _transactions);
            Console.WriteLine("Current Balance: {0}", _total);
            Console.Write("Command:");
        }

        private void Error()
        {
            Console.WriteLine();
            Console.WriteLine("Error: " + _errorMsg);
            Console.WriteLine("Press enter to retry");
            Console.ReadLine();
            Redraw();
        }

        private void ListEvents()
        {
            foreach (var evt in _eventList.Select(resolvedEvent => resolvedEvent.Event))
            {
                Console.WriteLine(evt.EventNumber + " : " + evt.EventType.ToLower() + " " + JObject.Parse(Encoding.UTF8.GetString(evt.Data)).Properties().First().Value);
            }
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
            Redraw();
        }
    }
}