using Application.Flow;
using Application.Weapons;
using Game;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Flow
{
    public class SessionWeaponBuildStoreTests
    {
        [Test]
        public void FailedSaveRemainsAuthoritativeAndRetryMakesItDurable()
        {
            var session = new PlayerSession();
            var durableStore = new FakeBuildStore { NextSaveResult = SaveResult.Failure("offline") };
            var store = new SessionWeaponBuildStore(session, durableStore);
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);

            SaveResult firstSave = store.Save(build);

            Assert.IsFalse(firstSave.IsSuccess);
            Assert.IsTrue(session.HasPendingSaves);
            Assert.AreSame(build, store.Load(WeaponWorkshopIds.Rifle).Build);

            durableStore.NextSaveResult = SaveResult.Success();
            Assert.AreEqual(1, store.RetryPendingSaves());
            Assert.IsFalse(session.HasPendingSaves);
            Assert.AreSame(build, session.GetCommittedBuild(WeaponWorkshopIds.Rifle));
        }

        private sealed class FakeBuildStore : IWeaponBuildStore
        {
            public SaveResult NextSaveResult = SaveResult.Success();

            public BuildLoadResult Load(string weaponId)
            {
                return BuildLoadResult.Failure("missing");
            }

            public SaveResult Save(WeaponBuild build)
            {
                return NextSaveResult;
            }
        }
    }
}
