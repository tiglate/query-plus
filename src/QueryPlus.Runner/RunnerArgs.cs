using QueryPlus.Domain.Enums;

namespace QueryPlus.Runner;

/// <summary>
/// Hand-rolled parser for the runner's command-line flags. Deliberately avoids
/// System.CommandLine - this is the single point of failure for a multi-hour job control loop,
/// so it stays dependency-light and its failure modes stay simple to reason about.
/// </summary>
public sealed class RunnerArgs
{
    public required int JobDefinitionId { get; init; }
    public required JobTriggerSource TriggeredBy { get; init; }
    public int? JobRunRequestId { get; init; }

    public static RunnerArgs Parse(string[] args)
    {
        int? jobDefinitionId = null;
        JobTriggerSource? triggeredBy = null;
        int? jobRunRequestId = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--job-definition-id":
                    jobDefinitionId = ParseIntValue(args, ref i, "--job-definition-id");
                    break;
                case "--triggered-by":
                    triggeredBy = ParseTriggerSourceValue(args, ref i);
                    break;
                case "--job-run-request-id":
                    jobRunRequestId = ParseIntValue(args, ref i, "--job-run-request-id");
                    break;
                default:
                    throw new ArgumentException($"Unrecognized argument: '{args[i]}'.");
            }
        }

        if (jobDefinitionId is null)
        {
            throw new ArgumentException("Missing required argument: --job-definition-id.");
        }

        if (triggeredBy is null)
        {
            throw new ArgumentException("Missing required argument: --triggered-by.");
        }

        return new RunnerArgs
        {
            JobDefinitionId = jobDefinitionId.Value,
            TriggeredBy = triggeredBy.Value,
            JobRunRequestId = jobRunRequestId
        };
    }

    private static int ParseIntValue(string[] args, ref int i, string flagName)
    {
        var value = NextValue(args, ref i, flagName);
        if (!int.TryParse(value, out var parsed))
        {
            throw new ArgumentException($"Argument '{flagName}' expects an integer value, got '{value}'.");
        }

        return parsed;
    }

    private static JobTriggerSource ParseTriggerSourceValue(string[] args, ref int i)
    {
        var value = NextValue(args, ref i, "--triggered-by");
        return value switch
        {
            "schedule" => JobTriggerSource.Schedule,
            "manual" => JobTriggerSource.Manual,
            _ => throw new ArgumentException(
                $"Argument '--triggered-by' expects 'schedule' or 'manual', got '{value}'.")
        };
    }

    private static string NextValue(string[] args, ref int i, string flagName)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Argument '{flagName}' requires a value.");
        }

        i++;
        return args[i];
    }
}
