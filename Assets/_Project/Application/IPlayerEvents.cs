using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IPlayerEvents
    {
        event Action Died;
    }
}
