
#if NET35
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.ComponentModel.DataAnnotations
{
	[System.AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	sealed class DisplayAttribute : Attribute
	{
		public string Name { get; set; }
        public Type ResourceType { get; set; }
	}
}
#endif
