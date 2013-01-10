using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Data.Linq;
using System.Linq;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Extension methods for the Accelerators project.  (rdm)
    /// </summary>
    public static class AcceleratorExtensions
    {
#region | BASE EXTENSIONS

        /// <summary>
        /// Returns an element attribute matching the specified name and converted into the expected data type.  Handles null values gracefully.
        /// </summary>
        /// <param name="element">Element whose attributes we are matching by name.</param>
        /// <param name="attributeName">Name of the attribute to return the value of the specified type.</param>
        /// <returns>Attribute value converted to the specifed type</returns>
        public static String GetAttribute(this XElement element, String attributeName)
        {
            return element.OnValid(e => e.Attribute(attributeName).OnValid(a => a.Value));
        }

        /// <summary>
        /// Performs the specified action on each element in the sequence handling null gracefully.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="sequence">Sequence of items.</param>
        /// <param name="action">The action to perform.</param>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if ( sequence != null )
                foreach ( T item in sequence )
                    action(item);
        }

        /// <summary>
        /// Evaluates the function only when the current object is not null. Provides in-line null value check and continue on valid.
        /// </summary>
        /// <typeparam name="T">Context value type.</typeparam>
        /// <typeparam name="U">Return value type.</typeparam>
        /// <param name="value">Context value.</param>
        /// <param name="func">Function to perform on non-null context value.</param>
        /// <returns>Results of function evaluation.</returns>
        public static U OnValid<T,U>(this T value, Func<T, U> func) where T : class
        {
            return value != null ? func(value) : default(U);
        }

        /// <summary>
        /// Evaluates the action only when the current object is not null. Provides in-line null value check and continue on valid.
        /// </summary>
        /// <typeparam name="T">Context value type.</typeparam>
        /// <param name="value">Context value.</param>
        /// <param name="action">Action to perform on non-null context value.</param>
        public static void OnValid<T>(this T value, Action<T> action) where T : class
        {
            if ( value != null ) action(value);
        }

        /// <summary>
        /// Evaluates the function only when value is not null; and then inside a try/catch block.  Use OnValid if you wish to handle exceptions.
        /// </summary>
        /// <typeparam name="T">Context value type.</typeparam>
        /// <typeparam name="U">Return value type.</typeparam>
        /// <param name="value">Context value.</param>
        /// <param name="func">Function to perform on non-null context value.</param>
        /// <returns>Results of function evaluation.</returns>
        public static void Protect<T>(this T value, Action<T> func) where T : class
        {
            if ( value != null )
                try { func(value); }
                catch {}
        }

        /// <summary>
        /// Evaluates the function only when value is not null; and then inside a try/catch block.  Use OnValid if you wish to handle exceptions.
        /// </summary>
        /// <typeparam name="T">Context value type.</typeparam>
        /// <typeparam name="U">Return value type.</typeparam>
        /// <param name="value">Context value.</param>
        /// <param name="func">Function to perform on non-null context value.</param>
        /// <returns>Results of function evaluation.</returns>
        public static U Protect<T, U>(this T value, Func<T, U> func) where T : class
        {
            if ( value != null )
                try { return func(value); }
                catch { }
            return default(U);
        }
        /// <summary>
        /// Performs type conversion of the value to the specified type handling nulls with default.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="value">The object.</param>
        /// <returns>Value converted to type T.</returns>
        public static T As<T>(this Object value) where T : IConvertible 
        {
            return value == null ? default(T) : (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Tests if the string is a valid value of the enum type.
        /// </summary>
        /// <typeparam name="T">The target enum type.</typeparam>
        /// <param name="value">The string value to test.</param>
        /// <returns>True if the value is a valid member of the enum.</returns>
        public static Boolean IsEnum<T>(this String value)
        {
            return Enum.IsDefined(typeof (T), value);
        }

        /// <summary>
        /// Returns the nullable enum value of the string for the provided enum type; otherwise null.
        /// </summary>
        /// <typeparam name="T">The target enum type.</typeparam>
        /// <param name="value">The string value for conversion.</param>
        /// <returns>Nullable of the enum type.</returns>
        public static T? ToEnum<T>(this String value) where T : struct
        {
            return (IsEnum<T>(value) ? (T?) Enum.Parse(typeof (T), value) : null);
        }

        /// <summary>
        /// Splits as string and returns a list of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The string to split.</param>
        /// <param name="settingSeparator">The separator.</param>
        /// <returns>List of type T.</returns>
        public static List<T> SplitToList<T>(this String value, Char[] settingSeparator) where T : IConvertible
        {
            return value.Split(settingSeparator, StringSplitOptions.RemoveEmptyEntries).Cast<T>().ToList();
        }

        /// <summary>
        /// Splits a string into pairs, then into keys and values, returning a dictionary.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="value">The string to splid.</param>
        /// <param name="pairsSeparator">The paired items separator.</param>
        /// <param name="valuesSeparator">The key value separator for each pair.</param>
        /// <returns>Dictionary</returns>
        public static Dictionary<TKey, TValue> SplitToDictionary<TKey,TValue>(this String value, Char[] pairsSeparator, Char[] valuesSeparator) where TKey : IConvertible where TValue : IConvertible
        {
            return value.SplitToList<String>(pairsSeparator).Select(pairs 
                => pairs.Split(valuesSeparator, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(kvp 
                    => kvp[0].As<TKey>(), kvp => kvp[1].As<TValue>());
        }

        /// <summary>
        /// Creates a string based respresentation of a dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary values.</param>
        /// <param name="keyFormat">The key format.</param>
        /// <param name="valueFormat">The value format.</param>
        /// <returns>
        /// String representation of the key value pairs.
        /// </returns>
        public static String ToString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, String keyFormat, String valueFormat)
        {
            if ( dictionary == null )
                return null;
            return String.Concat(dictionary.Select(x => String.Format("{0}{1}", String.Format(keyFormat, x.Key.ToString()), String.Format(valueFormat, x.Value.ToString()))).ToArray());
        }

        /// <summary>
        /// Creates a string based respresentation of a dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary values.</param>
        /// <param name="keyFormat">The key format.</param>
        /// <param name="valueFormat">The value format.</param>
        /// <returns>
        /// String representation of the key value pairs.
        /// </returns>
        public static String ToString(this System.Collections.IDictionary dictionary, String keyFormat, String valueFormat)
        {
            return dictionary.ToString(keyFormat, valueFormat);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that representation of the FileSecurity attributes. 
        /// </summary>
        /// <param name="security">The security.</param>
        /// <param name="format">The format - must contain: {0}, {1}, {2} identifiers.</param>
        /// <param name="includeExplicit">if set to <c>true</c> [include explicit].</param>
        /// <param name="includeInherited">if set to <c>true</c> [include inherited].</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public static String ToString(this FileSecurity security, String format, Boolean includeExplicit, Boolean includeInherited)
        {
            var sb  = new StringBuilder();

            var accessRules = security.GetAccessRules(includeExplicit, includeInherited, typeof (System.Security.Principal.SecurityIdentifier)).OfType<FileSystemAccessRule>().ToList();
            
            foreach (FileSystemAccessRule rule in accessRules)
            {
                sb.AppendFormat(format, 
                                String.Format("{{[\"{0}\"], '{1}'}}", 
                                              rule.IdentityReference.Value, 
                                              rule.AccessControlType),
                                String.Format("\r\n\t{{\r\n{0}\r\n\t}}\r\n\t{{\r\n{1}\r\n\t}}\r\n",
                                              rule.FileSystemRights.ToString("\t\t{{[\"{0}\"], '{1}'}},\r\n", true),
                                              rule.PropagationFlags.ToString("\t\t{{[\"{0}\"], '{1}'}},\r\n", true)));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance of propagation flags.
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <param name="kvpFormat">The KVP format.</param>
        /// <param name="includeImplicit">if set to <c>true</c> [include implicit].</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public static String ToString(this PropagationFlags flags, String kvpFormat, Boolean includeImplicit)
        {
            var sb = new StringBuilder();
            foreach ( PropagationFlags f in Enum.GetValues(typeof(PropagationFlags)) )
            {
                if ( includeImplicit || ( ( flags & f ) == f ) )
                    sb.AppendFormat(kvpFormat, f, ( flags & f ) == f);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance of file system rights.
        /// </summary>
        /// <param name="rights">The rights.</param>
        /// <param name="kvpFormat">The KVP format.</param>
        /// <param name="includeImplicit">if set to <c>true</c> [include implicit].</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public static String ToString(this FileSystemRights rights, String kvpFormat, Boolean includeImplicit)
        {
            var sb = new StringBuilder();
            foreach ( FileSystemRights r in Enum.GetValues(typeof(FileSystemRights)) )
            {
                if (includeImplicit || ((rights & r) == r))
                    sb.AppendFormat(kvpFormat, r, ( rights & r ) == r);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance of file attributes.
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="kvpFormat">The KVP format.</param>
        /// <param name="includeImplicit">if set to <c>true</c> [include implicit].</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public static String ToString(this FileAttributes attributes, String kvpFormat, Boolean includeImplicit)
        {
            var sb = new StringBuilder();
            foreach(FileAttributes a in Enum.GetValues(typeof(FileAttributes)))
            {
                if (includeImplicit || ((attributes & a) == a))
                    sb.AppendFormat(kvpFormat, a, (attributes & a) == a);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Replaces the path char.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="replacePathChar">The replacement char.</param>
        /// <remarks>If the supplied path char is a backslash, the assumption is that the path char to be replaced is the forward slash.  In all other instances the path char to be replaced is the backslash.</remarks>
        /// <returns></returns>
        public static String ReplacePathChar(this String path, Char replacePathChar)
        {
            if ( String.IsNullOrEmpty(path) )
                return path;
            return replacePathChar == '\\' ? path.Replace('/', '\\') : path.Replace('\\', replacePathChar);
        }

        /// <summary>
        /// Ensures that the supplied string ends with the specified characters. The characters are appended if not already existing.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="end">The end.</param>
        /// <returns></returns>
        public static String EnsureEndsWith(this String value, String end)
        {
            return value.EndsWith(end) ? value : value + end;
        }
        
#endregion      
#region | BINARY

        /// <summary>
        /// Write the binary field to a target file.
        /// </summary>
        public static void ToFile(this Binary binary, String targetFile)
        {
            if ( binary != null )
                File.WriteAllBytes(targetFile, binary.ToArray());
        }

        /// <summary>
        /// Write the binary field to a text string.
        /// </summary>
        public static String ToText(this Binary binary)
        {
            using ( var ms = new MemoryStream(binary.ToArray()) )
            using ( var sr = new StreamReader(ms) )
                return sr.ReadToEnd();
        }

        /// <summary>
        /// Write the binary field to an XDocument.
        /// </summary>
        public static XDocument ToXDocument(this Binary binary)
        {
            using ( var ms = new MemoryStream(binary.ToArray()) )
            using ( var sr = new XmlTextReader(ms) )
                return XDocument.Load(sr, LoadOptions.None);
        }

#endregion  
#region | NON-EXTENSIONS

        /// <summary>
        /// Starts a new process with the Accelerator environment.
        /// </summary>
        /// <param name="command">The file path.</param>
        /// <param name="workingDir">The working dir.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public static void RunProcess(String command, String workingDir, String args)
        {
            Process process = new Process
                                  {
                                      StartInfo =
                                          {
                                              RedirectStandardOutput = true,
                                              RedirectStandardError = true,
                                              RedirectStandardInput = false,
                                              UseShellExecute = false,
                                              CreateNoWindow = false, //true,
                                              WindowStyle = ProcessWindowStyle.Normal, // ProcessWindowStyle.Hidden,
                                              FileName = command,
                                              WorkingDirectory = workingDir,
                                              Arguments = args
                                          }
                                  };

            DataReceivedEventHandler outputHandler = (s, e) =>
            {
                var consoleOutput = new StringBuilder();
                if (!String.IsNullOrEmpty(e.Data))
                    try
                    {
                        consoleOutput.Append(e.Data);
                        Console.WriteLine(consoleOutput);
                    }
                    catch
                    {
                    }
            };

            //i| Set the console output handlers.
            process.ErrorDataReceived += outputHandler;
            process.OutputDataReceived += outputHandler;

            //i| Starting the process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }

#endregion
}