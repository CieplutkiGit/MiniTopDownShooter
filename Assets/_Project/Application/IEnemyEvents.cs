using System;

namespace Cieplutki.MiniTopDownShooter.Application
{
    public interface IEnemyEvents
    {
        event Action Died;
    }
}
