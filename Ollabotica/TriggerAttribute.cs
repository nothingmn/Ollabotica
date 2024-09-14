using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ollabotica;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TriggerAttribute : Attribute
{
    public string Trigger { get; set; }
    public string Description { get; set; }
    public bool IsAdmin { get; set; } = false;
}