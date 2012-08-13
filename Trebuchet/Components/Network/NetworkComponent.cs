using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Trebuchet.Interfaces;

namespace Trebuchet.Components.Network
{
    class NetworkComponent : IComponent
    {
        /// <summary>
        /// Main socket, to bind and call async methods from.
        /// </summary>
        public Socket InnerSocket { get; set; }

        public void Construct(IQueryable<XmlElement> Configuration)
        {
            XmlElement BindingElement = Configuration.Single(element => element.Name == "Binding");
            XmlElement ListenElement = Configuration.Single(element => element.Name == "Listening");

            var BindIP = Framework.CONFIG.GetValue<IPAddress>(BindingElement.GetAttributeNode("IP"));
            var BindPort = Framework.CONFIG.GetValue<int>(BindingElement.GetAttributeNode("Port"));
            var ListenBacklog = Framework.CONFIG.GetValue<int>(ListenElement.GetAttributeNode("Backlog"));

            // TODO: VALIDATE OBJECTS /\

            var EndPoint = new IPEndPoint(BindIP, BindPort);

            this.InnerSocket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.InnerSocket.Bind(EndPoint);
            this.InnerSocket.Blocking = false;
            this.InnerSocket.Listen(ListenBacklog);

            Framework.LOG.WriteLine("Listening on: {0}", this.InnerSocket.LocalEndPoint);
        }
    }
}
