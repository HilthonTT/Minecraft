namespace Minecraft.Core.Worlds.Biomes;

public sealed class BiomeProvider
{
    private const double MinimumClimateDistance = 1e-6;

    private readonly Biome[] _registeredBiomes;
    private readonly double[] _weights;
    private readonly BiomeMembership[] _memberships;

    public BiomeProvider(Biome[] registeredBiomes)
    {
        _registeredBiomes = registeredBiomes;
        _weights = new double[registeredBiomes.Length];
        _memberships = new BiomeMembership[registeredBiomes.Length];
    }

    public BiomeMembership[] GetBiomeMemberships(double temperature, double moisture)
    {
        double sum = 0;
        for (int i = 0; i < _registeredBiomes.Length; i++)
        {
            double temperatureDistance = Math.Abs(_registeredBiomes[i].Temperature - temperature);
            double moistureDistance = Math.Abs(_registeredBiomes[i].Moisture - moisture);
            double distance = Math.Max(temperatureDistance + moistureDistance, MinimumClimateDistance);

            double weight = 1 / (distance * distance * distance);
            _weights[i] = weight;
            sum += weight;
        }

        for (int i = 0; i < _registeredBiomes.Length; i++)
        {
            _memberships[i] = new BiomeMembership
            {
                Percentage = _weights[i] / sum,
                Biome = _registeredBiomes[i],
            };
        }

        return _memberships;
    }
}
