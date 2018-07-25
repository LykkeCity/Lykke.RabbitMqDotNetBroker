namespace DeduplicationSubscriberTest
{
    public class AppSettings
    {
        public string ConnString { get; set; }
        public string AlternateConnString { get; set; }
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string Header { get; set; }
        public string MongoDbConnString { get; set; }
        public string AzureConnString { get; set; }
    }
}
