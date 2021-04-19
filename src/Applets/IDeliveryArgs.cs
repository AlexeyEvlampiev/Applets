namespace Applets
{
    public interface IDeliveryArgs 
    {
        Applet SenderApplet { get; }
        MessageIntent MessageIntent { get; }
        object Data { get; }

        public bool IntentIs(MessageIntentId reference) => MessageIntent.Id.Equals(reference);
    }
}
