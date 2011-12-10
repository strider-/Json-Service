using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Collections;
using System.Reflection;

namespace JsonWebService
{
    /// <summary>
    /// A dynamic object that represents a json document, parsed from JsonDocument.Parse
    /// </summary>
    public class JsonDynamicDocument : DynamicObject
    {
        object obj;
        Type objType;

        internal JsonDynamicDocument(object jsonObject)
        {
            this.obj = jsonObject;
            IsHashtable = obj is Hashtable;
            IsArrayList = obj is ArrayList;
            objType = obj.GetType();
        }
        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            if(binder.Type == typeof(IEnumerable) && IsArrayList)
            {
                result = ((ArrayList)obj).ToArray().Select(o => new JsonDynamicDocument(o));
            }
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            object o = IsHashtable ? ((Hashtable)obj)[binder.Name] : null;

            if(o != null)
            {
                if(o is Hashtable || o is ArrayList)
                    result = new JsonDynamicDocument(o);
                else
                    result = o;
            }
            else
            {
                // invoke the member on the actual object, if it exists.
                MemberInfo[] mi = objType.GetMember(binder.Name);
                if(mi != null && mi.Length > 0)
                    result = objType.InvokeMember(binder.Name, BindingFlags.GetProperty | BindingFlags.GetField, null, obj, null);
                else
                    result = null;
            }

            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            // invoke the method on the actual object
            MethodInfo mi = objType.GetMethod(binder.Name);
            if(mi != null)
                result = mi.Invoke(obj, args);
            else
                result = null;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if(IsArrayList)
            {
                ArrayList al = obj as ArrayList;
                result = al[(int)indexes[0]];
                if(result is Hashtable)
                    result = new JsonDynamicDocument(result);
            }
            else
            {
                result = null;
            }

            return true;
        }

        public bool IsHashtable
        {
            get;
            private set;
        }
        public bool IsArrayList
        {
            get;
            private set;
        }
    }
}