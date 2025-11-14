using System;
using CookComputing.XmlRpc;

namespace OdooBackend.Interfaces
{
    public interface IOdooCommon : IXmlRpcProxy
    {
        [XmlRpcMethod("authenticate")]
        int Authenticate(string db, string username, string password, object args);
    }

}

