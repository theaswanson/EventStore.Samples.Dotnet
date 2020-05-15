using System;
using System.IO;

namespace AccountBalance
{
    /// <summary>
    /// A simple CQRS MVC Console application that demonstrates many of the features of the EventStore .net Client API
    /// The BalanceReadModel uses a catchup subscription with a checkpoint saved on disk
    /// The SessionStatsReadModel uses a live subscription to count events seen during the current session
    /// The Controller runs the command loop
    ///     The controller posts updates to the event store
    ///     Screen updates are triggered by changes in the data in the event store
    ///     Multiple concurrent instances can be running and all will seamlessly post and get updates
    ///     Other UIs can be built and attached at will with live updates
    /// Commands
    ///     credit & debit post events to a stream with the current checkpoint or NoStream as the expected position
    ///     repeat reads the last event posted to a stream and posts a new copy back to the stream using EventNumber as the expected position
    ///     repeat # reads a single event from a know position in a stream and posts a new copy back to the stream using ExpectedEvent.Any as the expected position
    ///     list reads all events in a stream forwards
    ///     rlist reads all events in a stream backwards
    ///     undo creates a reversal event to "erase" a mistake in an append only system
    ///     exit shuts down- leaving the EventStore running
    ///     clean shuts down- stopping the EventStore, Deleting the EventStore Data, and the Checkpoint file
    /// EventStoreLoader 
    ///     connects to or starts the event store as a separate process using the config file     
    ///     Enables Projections via config file
    ///     Starts the system projections using the projection manager
    ///     The currently embedded version of the EventStore is 3.1.0
    /// 
    /// N.B.: to launch the EventStore the Application needs to be run as an administrator,
    /// this means for interactive debugging your development environment needs to be running 
    /// as an administrator.
    /// If you are connecting to an already running event store Admin rights are not required. 
    /// </summary>
    class Program
    {
        private static BalanceReadModel _balanceRm;
        // ReSharper disable once NotAccessedField.Local
        private static SessionStatsReadModel _sessionStatsRm;
        private static ConsoleView _consoleView;
        private static Controller _controller;

        private const string StreamName = "Account";
        private const string ReadModelFile = "AccountCheckpoint.csv";

        static void Main()
        {
            Console.WriteLine("Loading EventStore...");

            EventStoreLoader.SetupEventStore();
            string privateCopy = CopyCheckpoint();

            _consoleView = new ConsoleView();
            _balanceRm = new BalanceReadModel(_consoleView, StreamName, privateCopy);
            _sessionStatsRm = new SessionStatsReadModel(_consoleView);
            _controller = new Controller(_consoleView, _balanceRm, StreamName, privateCopy);

            _controller.StartCommandLoop();

            UpdateCheckpoint(privateCopy);
        }

        private static void UpdateCheckpoint(string privateCopy)
        {
            if (File.Exists(privateCopy))
            {
                File.Copy(privateCopy, ReadModelFile, true);
                File.Delete(privateCopy);
            }
        }

        private static string CopyCheckpoint()
        {
            var privateCopy = Guid.NewGuid() + ".csv";
            if (File.Exists(ReadModelFile))
                File.Copy(ReadModelFile, privateCopy);
            return privateCopy;
        }
    }
}
