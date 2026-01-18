namespace Integrations.RabbitMQ;

public static class Constants
{
    public const string Exchange = "notifications.exchange";

    public static class RoutingKeys
    {
        public const string NotificationCreated = "notification.created";
        public const string SendEmail = "notification.send.email";
        public const string SendSms = "notification.send.sms";
        public const string SendPush = "notification.send.push";
    }

    public static class Queues
    {
        public const string Orchestrator = "notifications.orchestrator";
        public const string EmailWorker = "notifications.worker.email";
        public const string SmsWorker = "notifications.worker.sms";
        public const string PushWorker = "notifications.worker.push";
    }
}