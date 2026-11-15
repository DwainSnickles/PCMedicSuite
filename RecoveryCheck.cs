namespace PC_Medic;

/// <summary>
/// Proves a recovery stick while Windows still runs: the pointer has to parse, the image it names has
/// to be there at the size the stick recorded, and the archive has to pass wimlib's own integrity
/// check. A stick is only worth having if it is checked before it is needed, not after.
///
/// The one thing this cannot prove is booting into Windows Recovery, because that needs a restart.
/// </summary>
internal static class RecoveryCheck
{
    public sealed record Step(string Name, bool Ok, string Detail);

    public sealed record Result(bool Ok, string Summary, IReadOnlyList<Step> Steps);

    public static async Task<Result> RunAsync(
        string stickRoot,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var steps = new List<Step>();
        string root = (stickRoot ?? string.Empty).TrimEnd('\\') + "\\";

        // 1. The pointer is what a restore reads, so an unreadable or half-written one is a failure
        //    now rather than at the worst possible moment.
        string pointerFile = Path.Combine(root, BackupPointer.FolderName, BackupPointer.PointerFile);
        if (!File.Exists(pointerFile))
        {
            steps.Add(new Step("POINTER", false, $"{pointerFile} is missing — stamp this stick with BACK UP TO DISK."));
            return Verdict(steps);
        }

        BackupPointer.Pointer? pointer = BackupPointer.Read(root);
        if (pointer is null || string.IsNullOrWhiteSpace(pointer.ImagePath))
        {
            steps.Add(new Step("POINTER", false, $"{pointerFile} could not be read as a recovery pointer."));
            return Verdict(steps);
        }

        string firmware = string.IsNullOrWhiteSpace(pointer.Firmware) ? "boot mode not recorded" : pointer.Firmware;
        steps.Add(new Step(
            "POINTER",
            true,
            $"readable: image #{pointer.ImageIndex}, {firmware}, written {pointer.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm}."));

        // 2. The image itself: present, and the size the stick recorded. A mismatch means the capture
        //    never finished or the file was replaced since the stick was stamped.
        string image = pointer.ImagePath;
        if (!File.Exists(image))
        {
            steps.Add(new Step("IMAGE", false, $"{image} is not available — attach the disk the image was written to."));
            return Verdict(steps);
        }

        long bytes = new FileInfo(image).Length;
        if (pointer.ImageBytes > 0 && bytes != pointer.ImageBytes)
        {
            steps.Add(new Step(
                "IMAGE",
                false,
                $"{image} is {bytes:N0} bytes but the stick records {pointer.ImageBytes:N0} — " +
                "the backup did not finish, or the file was replaced."));
            return Verdict(steps);
        }

        steps.Add(new Step("IMAGE", true, $"{image} ({Theme.Bytes((ulong)bytes)}, {bytes:N0} bytes)."));

        // 3. The archive: wimlib reads it end to end, which is the only check that catches a bad sector
        //    or a half-copied file before an apply dies halfway through a disk.
        (bool archiveOk, string archiveMessage) = await ImagingService.VerifyAsync(image, progress, ct);
        steps.Add(new Step("ARCHIVE", archiveOk, archiveOk ? "wimlib verify passed: every resource hash matches." : archiveMessage));

        return Verdict(steps);
    }

    private static Result Verdict(List<Step> steps)
    {
        Step? failed = steps.FirstOrDefault(step => !step.Ok);

        return failed is null
            ? new Result(true, "STICK OK — POINTER, IMAGE AND ARCHIVE ALL CHECK OUT", steps)
            : new Result(false, $"{failed.Name} FAILED — {failed.Detail}", steps);
    }
}
