﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CmsData.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CmsData.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;FieldTypes&gt;
        ///  &lt;FieldType Name=&quot;Empty&quot; /&gt;
        ///  &lt;FieldType Name=&quot;Group&quot;&gt;
        ///    &lt;Comparison Type=&quot;AllTrue&quot; /&gt;
        ///    &lt;Comparison Type=&quot;AnyTrue&quot; /&gt;
        ///    &lt;Comparison Type=&quot;AllFalse&quot; /&gt;
        ///  &lt;/FieldType&gt;
        ///  &lt;FieldType Name=&quot;Bit&quot;&gt;
        ///    &lt;Comparison Type=&quot;Equal&quot; Display=&quot;{0} = {1}&quot; /&gt;
        ///    &lt;Comparison Type=&quot;NotEqual&quot; Display=&quot;{0} &amp;lt;&amp;gt; {1}&quot; /&gt;
        ///  &lt;/FieldType&gt;
        ///  &lt;FieldType Name=&quot;EqualBit&quot;&gt;
        ///    &lt;Comparison Type=&quot;Equal&quot; Display=&quot;{0} = {1}&quot; /&gt;
        ///  &lt;/FieldType&gt;
        ///  &lt;FieldType Name=&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CompareMap {
            get {
                return ResourceManager.GetString("CompareMap", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot;?&gt;
        ///&lt;fields&gt;
        ///  &lt;Grouping&gt;
        ///    &lt;Group Type=&quot;Group&quot;&gt;
        ///      Groups conditions or other groups together in an &quot;all true&quot; or &quot;any one true&quot; relationship
        ///    &lt;/Group&gt;
        ///  &lt;/Grouping&gt;
        ///  &lt;Personal&gt;
        ///    &lt;Age Type=&quot;NullInteger&quot;&gt;
        ///      Enter an age. Leave blank to find those without a DOB.
        ///    &lt;/Age&gt;
        ///    &lt;GenderId Title=&quot;Gender&quot; Type=&quot;Code&quot; DataSource=&quot;GenderCodes&quot; DataValueField=&quot;IdCode&quot;&gt;
        ///      Select Gender from the list.
        ///    &lt;/GenderId&gt;
        ///    &lt;MaritalStatusId Title=&quot;Mari [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FieldMap3 {
            get {
                return ResourceManager.GetString("FieldMap3", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {first},
        ///&lt;p&gt;We are very sorry, but something has gone wrong and your online giving transaction did not complete.&lt;/p&gt;
        ///&lt;p&gt;Please contact the church for help in resolving this issue.&lt;/p&gt;
        ///&lt;p&gt;Thank you.&lt;/p&gt;
        ///.
        /// </summary>
        internal static string ManagedGiving_FailedGivingMessage {
            get {
                return ResourceManager.GetString("ManagedGiving_FailedGivingMessage", resourceCulture);
            }
        }
    }
}
