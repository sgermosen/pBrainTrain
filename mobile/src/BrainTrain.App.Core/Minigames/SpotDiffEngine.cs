namespace BrainTrain.App.Core.Minigames;

/// <summary>
/// Encuentra las Diferencias: valida toques con coordenadas normalizadas
/// (0..1) contra las regiones de la escena. El radio de acierto es generoso
/// (dedos, no cursores). Las imágenes son 720×480: el radio vertical compensa
/// la relación de aspecto para que la zona sea un círculo en pantalla.
/// </summary>
public sealed class SpotDiffEngine(IReadOnlyList<SpotDiffScene>? scenes = null)
{
    public const double HitRadiusX = 0.075;
    public const double HitRadiusY = 0.075 * (720.0 / 480.0);

    private readonly IReadOnlyList<SpotDiffScene> _scenes = scenes ?? SpotDiffScenes.All;
    private readonly List<HashSet<int>> _found = [];
    private int _sceneIndex;

    public void Reset()
    {
        _found.Clear();
        for (var i = 0; i < _scenes.Count; i++) _found.Add([]);
        _sceneIndex = 0;
    }

    public SpotDiffScene Current => _scenes[_sceneIndex];
    public int SceneNumber => _sceneIndex + 1;
    public int SceneCount => _scenes.Count;
    public IReadOnlyCollection<int> FoundInScene => _found[_sceneIndex];
    public bool SceneComplete => _found[_sceneIndex].Count == Current.Diffs.Count;
    public bool IsLastScene => _sceneIndex == _scenes.Count - 1;

    /// <summary>Puntaje global: total de diferencias encontradas en todas las escenas.</summary>
    public int Score => _found.Sum(s => s.Count);

    /// <summary>Procesa un toque normalizado; devuelve el índice de la diferencia recién hallada o null.</summary>
    public int? TryTap(double x, double y)
    {
        for (var i = 0; i < Current.Diffs.Count; i++)
        {
            if (_found[_sceneIndex].Contains(i)) continue;
            var d = Current.Diffs[i];
            var dx = (x - d.X) / HitRadiusX;
            var dy = (y - d.Y) / HitRadiusY;
            if (dx * dx + dy * dy <= 1)
            {
                _found[_sceneIndex].Add(i);
                return i;
            }
        }
        return null;
    }

    public bool NextScene()
    {
        if (IsLastScene) return false;
        _sceneIndex++;
        return true;
    }
}
