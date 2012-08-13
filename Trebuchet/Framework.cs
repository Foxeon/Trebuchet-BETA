using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Trebuchet.Interfaces;
using System.Reflection;
using System.Net;

namespace Trebuchet
{
    static class Framework
    {
        /// <summary>
        /// Represents an collection with the created Components.
        /// </summary>
        public static ConcurrentDictionary<Type, IComponent> Components { get; private set; }

        /// <summary>
        /// The global/main solution.
        /// </summary>
        /// <param name="args">Program Arguments</param>
        public static void Main(string[] args)
        {
            Framework.CONFIG.Construct();
            Framework.LOG.Contruct(Framework.CONFIG.Get("Trebuchet.Framework.LOG"));

            Framework.LOG.WriteLine("Trebuchet booted static components.");

            Framework.ConstructComponents();
            Framework.LOG.WriteLine("Trebuchet booted ({0}) dynamic components.",Components.Count);

            Framework.RunApplicationAsync();
        }

        /// <summary>
        /// Returns an IComponent casted to a specified Type
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <returns>Casted IComponent</returns>
        public static T GetComponent<T>()
        {
            return (T)Components.Single(comp => comp.Key == typeof(T)).Value;
        }

        /// <summary>
        /// Constructs the framework/components.
        /// </summary>
        private static void ConstructComponents()
        {
            Framework.Components = new ConcurrentDictionary<Type, IComponent>();

            Assembly.GetExecutingAssembly().GetTypes().ToList().ForEach(type =>
                {
                    if (type.GetInterfaces().Contains(typeof(IComponent)))
                    {
                        IQueryable<XmlElement> Configuration = Framework.CONFIG.Get(type);

                        if (Configuration == default(IQueryable<XmlElement>))
                        {
                            Framework.LOG.WriteLine("{0} has no configuration.",type.Name);
                        }

                        IComponent Component = type.GetConstructor(new Type[] {}).Invoke(new object[] {}) as IComponent;

                        if (Framework.Components.TryAdd(type, Component) == false)
                        {
                            Framework.LOG.WriteCritical("Failed to add component: {0}", type.Name);
                        }
                        else
                        {
                            Component.Construct(Configuration);
                        }
                    }
                });
        }

        /// <summary>
        /// Runs the project async.
        /// </summary>
        private static async void RunApplicationAsync()
        {
            await Console.In.ReadToEndAsync();
        }

        #region LOGGIN COMPONENT 13-8-2012 (MEXTUR)
        public static class LOG
        {
            /// <summary>
            /// Thread-Safe wrapped console class.
            /// </summary>
            public static TextWriter Console { get; private set; }

            /// <summary>
            /// The color of the header.
            /// </summary>
            public static ConsoleColor HeaderColor { get; private set; }

            /// <summary>
            /// The format of the header.
            /// </summary>
            public static string HeaderValue { get; private set; }

            /// <summary>
            /// The color of the body
            /// </summary>
            public static ConsoleColor BodyColor { get; private set; }

            /// <summary>
            /// The title of the console; that can be edited.
            /// </summary>
            public static string ConsoleTitle
            {
                get { return System.Console.Title; }
                set { System.Console.Title = value; }
            }

            /// <summary>
            /// Constructs this component.
            /// </summary>
            /// <param name="Configuration">Configuration parameters</param>
            public static void Contruct(IQueryable<XmlElement> Configuration)
            {
                LOG.Console = TextWriter.Synchronized(System.Console.Out);
                LOG.Console.WriteLineAsync();

                XmlElement ConsoleElement = Configuration.Single(element => element.Name == "Console");
                XmlElement HeaderElement = Configuration.Single(element => element.Name == "Header");
                XmlElement BodyElement = Configuration.Single(element => element.Name == "Body");

                LOG.ConsoleTitle = Framework.CONFIG.GetValue<string>(ConsoleElement.GetAttributeNode("Title"));
                LOG.HeaderColor = Framework.CONFIG.GetValue<ConsoleColor>(HeaderElement.GetAttributeNode("Color"));
                LOG.HeaderValue = Framework.CONFIG.GetValue<string>(HeaderElement.GetAttributeNode("Designer"));
                LOG.BodyColor = Framework.CONFIG.GetValue<ConsoleColor>(BodyElement.GetAttributeNode("Color"));
            }

            /// <summary>
            /// Returns the called classname, used for the logging.
            /// </summary>
            /// <returns>Called Classname</returns>
            private static string GetCallingType()
            {
                return new StackFrame(5, false).GetMethod().DeclaringType.Name;
            }

            /// <summary>
            /// Writes a string to the console asynchronously.
            /// </summary>
            /// <param name="Line">String to write</param>
            /// <param name="Parameters">Object parameters</param>
            public static async void WriteLine(string Line, params object[] Parameters)
            {
                System.Console.ForegroundColor = LOG.HeaderColor;
                await LOG.Console.WriteAsync(" " + 
                    HeaderValue.Replace(
                    "@time", DateTime.Now.ToString()).Replace(
                    "@type", GetCallingType().ToString()) + " ");
                System.Console.ForegroundColor = LOG.BodyColor;
                await LOG.Console.WriteLineAsync(string.Format(Line, Parameters));
            }

            /// <summary>
            /// Writes a string to the console asynchronously + critical
            /// </summary>
            /// <param name="Line">String to write</param>
            /// <param name="Parameters">Object parameters</param>
            public static async void WriteCritical(string Line, params object[] Parameters)
            {
                System.Console.ForegroundColor = LOG.HeaderColor;
                await LOG.Console.WriteAsync(" " +
                    HeaderValue.Replace(
                    "@time", DateTime.Now.ToString()).Replace(
                    "@type", GetCallingType().ToString()) + " ");
                System.Console.ForegroundColor = ConsoleColor.Red;
                await LOG.Console.WriteLineAsync(string.Format(Line, Parameters));
            }
        }
        #endregion

        #region CONFIGURATION COMPONENT 13-8-2012 (MEXTUR)
        public static class CONFIG
        {
            /// <summary>
            /// The document of the config-xml.
            /// </summary>
            public static XmlDocument ConfigFile { get; private set; }

            /// <summary>
            /// The collection of the dynamic configuration.s
            /// </summary>
            public static ConcurrentDictionary<Type, IQueryable<XmlElement>> DynamicConfiguration { get; private set; }

            /// <summary>
            /// The collection of the static configuration.s
            /// </summary>
            public static ConcurrentDictionary<string, IQueryable<XmlElement>> StaticConfiguration { get; private set; }
            
            /// <summary>
            /// Constructs this component.
            /// </summary>
            public static void Construct()
            {
                CONFIG.ConfigFile = new XmlDocument();
                CONFIG.ConfigFile.Load("Trebuchet.xml");

                CONFIG.DynamicConfiguration = new ConcurrentDictionary<Type, IQueryable<XmlElement>>();
                CONFIG.StaticConfiguration = new ConcurrentDictionary<string, IQueryable<XmlElement>>();

                LoadComponentConfiguration();
            }

            /// <summary>
            /// Caches all settings into the collections.
            /// </summary>
            public static void LoadComponentConfiguration()
            {
                foreach (XmlElement Component in ConfigFile.SelectNodes("/Trebuchet/Framework/Components/*"))
                {
                    ICollection<XmlElement> Elements = new List<XmlElement>();

                    foreach (XmlElement Element in Component.ChildNodes)
                    {
                        Elements.Add(Element);
                    }

                    if (Component.GetAttribute("Type") == "Static")
                    {
                        StaticConfiguration.TryAdd(Component.GetAttribute("Namespace"), Elements.AsQueryable());
                        continue;
                    }

                    if (string.IsNullOrEmpty(Component.GetAttribute("Namespace")))
                    {
                        Framework.LOG.WriteCritical("Found component without 'namespace': {0}", Component.LocalName);
                        continue;
                    }

                    Type Type = Type.GetType(Component.GetAttribute("Namespace"), false);
                    DynamicConfiguration.TryAdd(Type, Elements.AsQueryable());
                }
            }

            /// <summary>
            /// Converts the attribute to the value of an specified type.
            /// </summary>
            /// <typeparam name="T">Type</typeparam>
            /// <param name="Attribute">Input Attribute</param>
            /// <returns>Converted Object</returns>
            public static T GetValue<T>(XmlAttribute Attribute)
            {
                try
                {
                    if (typeof(T) == typeof(ConsoleColor))
                    {
                        return (T)Enum.Parse(typeof(ConsoleColor), Attribute.Value);
                    }

                    if (typeof(T) == typeof(int))
                    {
                        return (T)(object)int.Parse(Attribute.Value);
                    }

                    if (typeof(T) == typeof(IPAddress))
                    {
                        return (T)(object)IPAddress.Parse(Attribute.Value);
                    }
                }
                catch (Exception)
                {
                    Framework.LOG.WriteCritical("Failed to convert attribute: {0}", Attribute.Name);
                }

                return (T)(object)Attribute.Value;
            }

            /// <summary>
            /// Represents an IQueryable for a dynamic component.
            /// </summary>
            /// <param name="Type">Type of Component</param>
            /// <returns>Configuration of component</returns>
            public static IQueryable<XmlElement> Get(Type Type)
            {
                return CONFIG.DynamicConfiguration.SingleOrDefault(element => element.Key == Type).Value;
            }

            /// <summary>
            /// Represents an IQueryable for a static component.
            /// </summary>
            /// <param name="Type">XPath of Component</param>
            /// <returns>Configuration of component</returns>
            public static IQueryable<XmlElement> Get(string Type)
            {
                return CONFIG.StaticConfiguration.SingleOrDefault(element => element.Key == Type).Value;
            }
        }
        #endregion
    }
}
