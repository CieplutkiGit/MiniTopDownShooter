using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Weapons
{
    public sealed class BuildResolution
    {
        public bool IsValid { get; }
        public ResolvedWeaponStats Stats { get; }
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyList<string> AffectedSlotOrPartIds { get; }

        private BuildResolution(bool isValid, ResolvedWeaponStats stats, IEnumerable<string> errors, IEnumerable<string> affectedIds)
        {
            IsValid = isValid;
            Stats = stats;
            Errors = (errors != null ? errors.ToList() : new List<string>()).AsReadOnly();
            AffectedSlotOrPartIds = (affectedIds != null ? affectedIds.ToList() : new List<string>()).AsReadOnly();
        }

        public static BuildResolution Success(ResolvedWeaponStats stats)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            return new BuildResolution(true, stats, null, null);
        }

        public static BuildResolution Failure(IEnumerable<string> errors, IEnumerable<string> affectedIds = null)
        {
            return new BuildResolution(false, null, errors, affectedIds);
        }

        public static BuildResolution Failure(string error, string affectedId = null)
        {
            var errors = new[] { error };
            var ids = affectedId != null ? new[] { affectedId } : null;
            return new BuildResolution(false, null, errors, ids);
        }
    }

    public sealed class BuildLoadResult
    {
        public bool IsSuccess { get; }
        public WeaponBuild Build { get; }
        public bool WasMigratedOrRepaired { get; }
        public string ErrorMessage { get; }

        private BuildLoadResult(bool isSuccess, WeaponBuild build, bool wasMigratedOrRepaired, string errorMessage)
        {
            IsSuccess = isSuccess;
            Build = build;
            WasMigratedOrRepaired = wasMigratedOrRepaired;
            ErrorMessage = errorMessage ?? "";
        }

        public static BuildLoadResult Success(WeaponBuild build, bool wasMigratedOrRepaired = false)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            return new BuildLoadResult(true, build, wasMigratedOrRepaired, null);
        }

        public static BuildLoadResult Failure(string errorMessage)
        {
            return new BuildLoadResult(false, null, false, errorMessage);
        }
    }

    public sealed class ApplyResult
    {
        public bool IsSuccess { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public IReadOnlyList<string> AffectedSlotOrPartIds { get; }

        private ApplyResult(bool isSuccess, string errorCode, string errorMessage, IEnumerable<string> affectedIds)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode ?? "";
            ErrorMessage = errorMessage ?? "";
            AffectedSlotOrPartIds = (affectedIds != null ? affectedIds.ToList() : new List<string>()).AsReadOnly();
        }

        public static ApplyResult Success()
        {
            return new ApplyResult(true, null, null, null);
        }

        public static ApplyResult Failure(string errorCode, string errorMessage, IEnumerable<string> affectedIds = null)
        {
            return new ApplyResult(false, errorCode, errorMessage, affectedIds);
        }

        public static ApplyResult Failure(string errorCode, string errorMessage, string affectedId)
        {
            var ids = affectedId != null ? new[] { affectedId } : null;
            return new ApplyResult(false, errorCode, errorMessage, ids);
        }
    }

    public sealed class SaveResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private SaveResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage ?? "";
        }

        public static SaveResult Success()
        {
            return new SaveResult(true, null);
        }

        public static SaveResult Failure(string errorMessage)
        {
            return new SaveResult(false, errorMessage);
        }
    }
}
