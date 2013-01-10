using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.WindowsAzure.Diagnostics;

namespace Microsoft.WindowsAzure.Accelerator.Diagnostics
{
    /// <summary>
    /// Static wrapper methods to abstracting trace configuration, usage and formatting.
    /// </summary>
    public static class Tracing
    {
#region | TEMPLATES

        //x| private const String _ExceptionFormat  = "[( {0} )]\r\n[( MESSAGE )]\r\n{1}\r\n[( TYPE )]\r\n{2}\r\n[( SOURCE )]\r\n{3}\r\n[( TARGET SITE )]\r\n{4}\r\n[( DATA )]\r\n{5}\r\n[( STACK TRACE )]\r\n{6}";

        /// <summary>
        /// Content items offset (indent).
        /// </summary>
        private static String _Offset = "\t";

        /// <summary>
        /// Content items separator template.
        /// </summary>
        private static String _ContentSeparator = "\r\n";


        private static String _MessageSeparator = " : ";

        ///// <summary>
        ///// Standard single line trace message format.
        ///// {0} : Section Name
        ///// {1} : Message
        ///// </summary>
        //public static TemplateFormat MessageTemplate = "{0} : {1}";

        /// <summary>
        /// Header or section name format template.
        /// {0} : Separator         (Separator)
        /// {1} : Indent            (IndentFormat)
        /// {2} : Title
        /// </summary>
        public static TemplateFormat HeaderTemplate = "{0}{1}[( {2} )]";

        ///// <summary>
        ///// Header or section name format template.
        ///// {0} : Separator         (Separator)
        ///// {1} : Indent            (IndentFormat)
        ///// {2} : Content
        ///// </summary>
        //public static TemplateFormat ContentTemplate = "{0}{1}{2}";

        /// <summary>
        /// Name and value format template.
        /// {0} : Separator         (Separator)
        /// {1} : Indent            (IndentFormat)
        /// {2} : Key
        /// {3} : Value
        /// </summary>
        public static TemplateFormat KeyValueTemplate = "{0}{1}{2,-30}:  {3}";
        
        /// <summary>
        /// Exception format template.
        /// {0} : Exception Title   (HeaderFormat)
        /// {1} : Message           (KvpFormat)
        /// {2} : Type              (KvpFormat)
        /// {3} : Source            (KvpFormat)
        /// {4} : Target Site       (KvpFormat)
        /// {5} : Data              (KvpFormat)
        /// {6} : Stack Trace       (KvpFormat)
        /// {7} : InnerException    (ExceptionFormat)
        /// </summary>
        public static TemplateFormat ExceptionTemplate = "{0}{1}{2}{3}{4}{5}{6}{7}";

#endregion
#region | TRACING

        public static void TraceException(this LogLevel logLevel, String sectionName, Exception ex) { logLevel.TraceException(sectionName, ex, null); }
        public static void TraceException(this LogLevel logLevel, String sectionName, Exception ex, TemplateFormat message, params object[] messageArgs)
        {
            logLevel.Trace(FormatMessage(sectionName, message.Render(messageArgs), ex.FormatException()));
        }

        public static void TraceContent(this LogLevel logLevel, String sectionName, String content, TemplateFormat message, params object[] messageArgs)
        {
            logLevel.Trace(FormatMessage(sectionName, message.Render(messageArgs), content));
        }

        public static void Trace(this LogLevel logLevel, String sectionName, TemplateFormat message, params object[] messageArgs)
        {
            logLevel.Trace(FormatMessage(sectionName, message.Render(messageArgs))); 
        }

        public static void Trace(this LogLevel logLevel, String message)
        {
            if ( DiagnosticsSettings.IsLoggingEnabled && DiagnosticsSettings.TraceSource != null )
            {
                //i|
                //b| Write to log.
                //i|
                DiagnosticsSettings.TraceSource.TraceEvent(logLevel.ToTraceEvent(), 0, message);
                DiagnosticsSettings.TraceSource.Flush();
                System.Diagnostics.Trace.Flush();
            }
            else
            {
                switch ( logLevel )
                {
                    case LogLevel.Warning    : System.Diagnostics.Trace.TraceWarning(message); break;
                    case LogLevel.Error      : System.Diagnostics.Trace.TraceError(message); break;
                    case LogLevel.Information:
                    case LogLevel.Verbose    :
                    default                  : System.Diagnostics.Trace.TraceInformation(message); break;
                }
                System.Diagnostics.Trace.Flush();
            }
        }

#endregion
#region | EXTENSION METHODS

        /// <summary>
        /// Converts the LogLevel into an appropriate TraceEventType.
        /// </summary>
        public static TraceEventType ToTraceEvent(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Undefined:    return TraceEventType.Information;
                case LogLevel.Verbose:      return TraceEventType.Verbose;
                case LogLevel.Information:  return TraceEventType.Information;
                case LogLevel.Warning:      return TraceEventType.Warning;
                case LogLevel.Error:        return TraceEventType.Error;
                case LogLevel.Critical:     return TraceEventType.Critical;
                default:                    return TraceEventType.Information;
            }
        }

        /// <summary>
        /// Returns a string representation of all of an object public property kev/value pairs (using reflection).
        /// </summary>
        public static String ToTraceString<T>(this T value) { return value.ToTraceString(_ContentSeparator, _Offset); }
        public static String ToTraceString<T>(this T value, String separator, String offset)
        {
            if ( !( value is ValueType ) && value == null )
                return "null";
            StringBuilder sb = new StringBuilder();
            foreach ( var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).Select(p => p) )
            {
                String k = property.Name;
                String v = property.Protect(p => p.GetValue(value, null).ToString()) ?? "?";
                sb.Append(FormatKeyValue(k, v, separator, offset));
                //sb.AppendFormat(template, property.Name + ( separator ?? String.Empty ), property.Protect(p => p.GetValue(value, null)) ?? "?");
            }
            return sb.ToString();
        }

        public static String ToTraceString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) { return dictionary.ToTraceString(_ContentSeparator, _Offset); }
        public static String ToTraceString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, String title) { return dictionary.ToTraceString(title, _ContentSeparator, _Offset); }
        public static String ToTraceString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, String title, String separator, String offset)
        {
            if ( dictionary == null )
                return "null";
            return FormatContent(
                title, 
                dictionary.Select(kvp => FormatKeyValue(kvp.Key.ToString(), kvp.Value.ToString(), separator, offset)).Aggregate((v, a) => v + a),
                separator,
                offset
                );
        }

        public static String FormatException(this Exception e) { return FormatException(e, "EXCEPTION", _ContentSeparator, _Offset); }
        public static String FormatException(this Exception e, String title, String separator, String offset)
        {
            return ExceptionTemplate.Render(
                FormatContentHeader(title, separator, offset),
                FormatKeyValue("Message", e.Message, separator, offset),
                FormatKeyValue("Type", e.GetType().Name, separator, offset),
                FormatKeyValue("Source", e.Source, separator, offset),
                FormatKeyValue("Target Site", e.TargetSite.ToTraceString(), separator, offset),
                FormatKeyValue("Data", e.Data.ToTraceString(), separator, offset),
                FormatKeyValue("StackTrace", e.StackTrace, separator, offset),
                e.InnerException != null ? e.InnerException.FormatException("INNER EXCEPTION", separator, offset + _Offset) : String.Empty
                );
        }

#endregion
#region | FORMATTING

        public static String FormatMessage(params object[] args)
        {
            if ( args == null )
                return "null";
            return args.Select(a => a.ToString()).Aggregate((a, r) => a + _MessageSeparator + r);
        }

        public static String FormatContent(String content) { return FormatContent(null, content); }
        public static String FormatContent(String header, String content) { return FormatContent(header, content, _ContentSeparator, _Offset); }
        public static String FormatContent(String header, String content, String separator, String offset)
        {
            return String.IsNullOrEmpty(header) ? content : HeaderTemplate.Render(separator, offset, header) + content;
            //x| return title + HeaderTemplate.Render(separator, offset, header);
        }
        
        public static String FormatKeyValue<T>(String name, T value) { return FormatKeyValue<T>(name, value, _ContentSeparator, _Offset); }
        public static String FormatKeyValue<T>(String name, T value, String separator, String offset)
        {
            return KeyValueTemplate.Render(separator, offset, name, value);
        }

        public static String FormatContentHeader(String title) { return FormatContentHeader(title, _ContentSeparator, _Offset); }
        public static String FormatContentHeader(String title, String separator, String offset)
        {
            return HeaderTemplate.Render(separator, offset, title);
        }
        
#endregion

    }

    public class TemplateFormat
    {

#region | PROPERTIES

        public String Template {  get; private set;}
        public IFormatProvider FormatProvider { get; private set; }

#endregion
#region | CONSTRUCTORS

        public TemplateFormat(String template)
        {
            Template = template ?? String.Empty;
        }

        public TemplateFormat(String template, IFormatProvider formatProvider) : this(template)
        {
            FormatProvider = formatProvider;
        }

        public static implicit operator TemplateFormat(String value)
        {
            return new TemplateFormat(value);
        }

        public static implicit operator String(TemplateFormat templateFormat)
        {
            return templateFormat.Template;
        }

#endregion
#region | RENDER

        public String Render(params object[] args)
        {
            return Render(FormatProvider, args);
        }

        public String Render(IFormatProvider provider, params object[] args)
        {
            if (args == null)
                return Template;
            if ( String.IsNullOrEmpty(Template) )
                return String.Concat(args);
            if (provider == null)
                return String.Format(Template, args);
            return String.Format(provider, Template, args);
        }

        public override string ToString()
        {
            return Template;
        }

#endregion

    }
}