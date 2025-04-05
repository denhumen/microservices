namespace ConfigServer.Models
{
    public class RegistrationRequest
    {
        public required string ServiceName { get; set; }
        public required string Url { get; set; }
    }
}
