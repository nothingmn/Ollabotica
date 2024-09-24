using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica;

public class ChatMessage
{
    public string IncomingText { get; set; }
    public string OutgoingText { get; set; }
    public string UserIdentity { get; set; }
    public string MessageId { get; set; }
    public string ChatId { get; set; }
    public object Channel { get; set; }

    public DateTimeOffset Received { get; set; }
}