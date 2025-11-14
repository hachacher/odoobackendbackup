using System;
using CookComputing.XmlRpc;

namespace OdooBackend.Interfaces
{
    public interface IOdooObject : IXmlRpcProxy
    {
        [XmlRpcMethod("execute_kw")]
        object[] SearchRead(string db, int uid, string password,
            string model, string method, object[] args, object kwargs);
    }

}

