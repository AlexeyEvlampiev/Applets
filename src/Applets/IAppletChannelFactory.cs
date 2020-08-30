using System;

namespace Applets
{
    public interface IAppletChannelFactory
    {
        IAppletChannel Create(Guid appletId);
    }
}
