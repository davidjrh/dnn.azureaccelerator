using System;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Accelerator.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Diagnostics;

namespace Microsoft.WindowsAzure.Accelerator
{
    /// <summary>
    /// Class provides support for tokenized definition of interdependant configuration values. Token are in the form
    /// of $(Key), where the key value is used to resolve to additional variable values in the global namespace.  This
    /// process continues until all keys have been resolved and the final string representation of the variable can be 
    /// returned. - (i|rdm)
    /// </summary>
    public class Variable
    {
#region | FIELDS

        /// <summary>
        /// Regex for variable token replacement. 
        /// </summary>
        private readonly static Regex _KeyVariableMatch = new Regex(@"\$\((?<Key>\w*)\)", RegexOptions.Compiled);
        
#endregion
#region | PROPERTIES

        /// <summary>
        /// Gets or sets the variable value including tokenized keys.
        /// </summary>
        public String TokenizedValue { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether the variable contains keys for additional variables.
        /// </summary>
        public Boolean ContainsKeys { get { return !_KeyVariableMatch.IsMatch(TokenizedValue); } }

#endregion
#region | CONSTRUCTORS

        /// <summary>
        /// Creates a new variable instance using the supplied string (which may contain tokenized values).
        /// </summary>
        /// <param name="tokenizedString">The tokenized string.</param>
        public Variable(String tokenizedString)
        {
            TokenizedValue = tokenizedString ?? String.Empty;
        }

#endregion
#region | IMPLICIT OPERATORS

        /// <summary>
        /// Creates a new variable instance using the supplied string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>The variable.</returns>
        public static implicit operator Variable(String value)
        {
            //i| Variables may contain variables.
            return new Variable(value);
        }

        /// <summary>
        /// Returns a boolean representation of the variable. An exception will be thrown if the fully resolved underlying value is not a valid boolean string.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Boolean conversion of variable string.</returns>
        public static implicit operator Boolean(Variable variable)
        {
            return String.IsNullOrEmpty(variable.ToString()) ? false : Boolean.Parse(variable.ToString());
        }

        /// <summary>
        /// Returns a integer representation of the variable. An exception will be thrown if the fully resolved underlying value is not a valid integer string.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Int32 conversion of variable string.</returns>
        public static implicit operator Int32(Variable variable)
        {
            return Int32.Parse(variable.ToString());
        }
        
        /// <summary>
        /// Performs an implicit conversion from <see cref="Microsoft.WindowsAzure.Accelerator.Variable"/> to <see cref="System.String"/>. Resolves 
        /// any tokens in this variable string and returns the result as a new variable string.  This process will continue until a string without 
        /// tokens is returned.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator String(Variable variable)
        {
            return variable.Render(s => s);
            /*x|  _KeyVariableMatch.Replace(variable.TokenizedValue, (match) =>
                                                                          {
                                                                              String key = match.Groups["Key"].Value;
                                                                              Variable value = null;
                                                                              if (!ServiceManager.Variables.TryGetValue(key, out value))
                                                                                  try
                                                                                  {
                                                                                      value = RoleEnvironment.GetConfigurationSettingValue(key);
                                                                                  }
                                                                                  catch
                                                                                  {
                                                                                      LogLevel.Verbose.Trace("Variable", "Unable to resolve key '{0}' : Using key as value.", key);
                                                                                      value = key;
                                                                                  }
                                                                              return value;
                                                                          });*/
        }

#endregion
#region | OVERRIDES

        /// <summary>
        /// Explicitly call of the implicit string conversion.  Not really needed but included for completeness.
        /// </summary>
        /// <returns>String represenation of the variable with all of the tokens resovled.</returns>
        public override string ToString()
        {
            return (String)this;
        }

#endregion
#region | METHODS

        /// <summary>
        /// Renders the specified variable using the provided function 
        /// </summary>
        /// <param name="encoder">The render value.</param>
        /// <returns></returns>
        public String Render(Func<String, String> encoder)
        {
            return  _KeyVariableMatch.Replace(TokenizedValue, (match) =>
                                                                          {
                                                                              String key = match.Groups["Key"].Value;
                                                                              Variable value = null;
                                                                              if (!ServiceManager.Variables.TryGetValue(key, out value))
                                                                                  try
                                                                                  {
                                                                                      value = RoleEnvironment.GetConfigurationSettingValue(key);
                                                                                  }
                                                                                  catch
                                                                                  {
                                                                                      LogLevel.Verbose.Trace("Variable", "Unable to resolve key '{0}' : Using key as value.", key);
                                                                                      value = key;
                                                                                  }
                                                                              //i| Apply render method to the fully resolved variable only; otherwise
                                                                              //i| the process of escaping may short-curcuit resolution.
                                                                              return value.ContainsKeys ? (String)value : encoder(value);
                                                                          });
            
        }

#endregion
    }
}