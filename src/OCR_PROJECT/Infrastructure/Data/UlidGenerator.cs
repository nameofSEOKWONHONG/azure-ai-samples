using NUlid;
using NUlid.Rng;

namespace Document.Intelligence.Agent.Infrastructure.Data;

public class UlidGenerator
{
    private static Lazy<UlidGenerator> _instance = new Lazy<UlidGenerator>(() => new UlidGenerator());
    public static UlidGenerator Instance => _instance.Value;
    
    private UlidGenerator(){}

    private MonotonicUlidRng _rng = new MonotonicUlidRng();
    public Ulid Generate() => Ulid.NewUlid(_rng);
    public string GenerateString() => Ulid.NewUlid(_rng).ToString();
    public Ulid Generate(DateTimeOffset offset) => Ulid.NewUlid(offset);
}