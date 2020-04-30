using System;

namespace MashupApi.Http.Clients.Exceptions
{
    [System.Serializable]
    public class MusicBrainzNotFoundException : System.Exception
    {
        public MusicBrainzNotFoundException() { }
        public MusicBrainzNotFoundException(string message) : base(message) { }
        public MusicBrainzNotFoundException(string message, System.Exception inner) : base(message, inner) { }
        protected MusicBrainzNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
