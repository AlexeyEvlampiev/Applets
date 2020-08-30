using System;

namespace Applets.Common
{
    public sealed class AppletInfo
    {
        public AppletInfo(Guid id, string name)
        {
            Id = (id != Guid.Empty) ? id : throw new ArgumentException($"Aplet id is required."); ;
            Name = !String.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException($"Aplet name is required.");
        }

        public Guid Id { get; }
        public string Name { get; }
    }
}
