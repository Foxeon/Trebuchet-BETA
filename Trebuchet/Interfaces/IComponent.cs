using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Trebuchet.Interfaces
{
    interface IComponent
    {
        /// <summary>
        /// Constructs this component dynamicly.
        /// </summary>
        /// <param name="Configuration">Configuration parameters</param>
        void Construct(IQueryable<XmlElement> Configuration);
    }
}
