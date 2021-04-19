using System;

namespace Applets
{
    public sealed record EventKey(MessageIntentId EventIntentId, Type DtoType);
}
