using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica.OutputProcessors.Assistant;
public class CreateReminder : TaskBase
{
    public string title { get; set; }
    public DateTime dueDate { get; set; }
    public DateTime endTime { get; set; }
}


public class DeleteReminder : TaskBase
{
    public string Title { get; set; }
    public string Status { get; set; }
}

public class TaskBase
{
    public string Task { get; set; }
}


public class CreateTimer : TaskBase
{
    public string Duration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}



public class DeleteTimer : TaskBase
{
    public string TimerId { get; set; }
    public string Status { get; set; }
}


public class CreateEvent : TaskBase
{
    public string Subject { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public List<string> InviteList { get; set; }
}



public class SendMessage : TaskBase
{
    public string Recipient { get; set; }
    public string Subject { get; set; }
    public string Status { get; set; }
}



public class SendEmail : TaskBase
{
    public List<string> Recipients { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Status { get; set; }
}
