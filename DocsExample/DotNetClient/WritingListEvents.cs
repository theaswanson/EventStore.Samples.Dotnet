using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Text;

namespace DocsExample
{
    public class WritingListEvents
    {
        private static readonly IEventStoreConnection conn = null;

        private static EventData CreateSample(int i)
        {
            var sampleObject = new { a = i };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sampleObject));
            var metadata = Encoding.UTF8.GetBytes("{}");
            var eventPayload = new EventData(Guid.NewGuid(), "event-type", true, data, metadata);
            return eventPayload;
        }

        public static void Method()
        {
            conn.AppendToStreamAsync("newstream", ExpectedVersion.Any, new[] { CreateSample(1), CreateSample(2), CreateSample(3) }).Wait();
        }
    }
}