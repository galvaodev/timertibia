namespace TimerApp.Core.Models;

// "record" em C# = objeto imutável, como as suas props no React
// Uma vez criado, os valores não mudam — pra alterar, você cria um novo

public record TimerConfig
{
    // Guid = UUID — gerado automaticamente se não passar nenhum
    public string Id { get; init; } = Guid.NewGuid().ToString();

    // init = só pode setar na criação, depois é read-only
    // Igual ao "readonly" do TypeScript
    public string Name { get; init; } = string.Empty;

    public TimerCategory Category { get; init; } 

    // TimeSpan = tipo nativo do C# pra representar duração
    // Não existe no JS — substitui o "número de milissegundos"
    public TimeSpan Duration { get; init;}

    public bool IsLooping { get; init;}

    // Sons default por categoria — calculado automaticamente
    // "=>" sem chaves = expression body, igual arrow function
    public string SoundFile => Category switch
    {
        TimerCategory.Food   => "Assets/sounds/food.mp3",
        TimerCategory.Boost  => "Assets/sounds/boost.mp3",
        TimerCategory.Potion => "Assets/sounds/potion.mp3",
        _                    => "Assets/sounds/default.mp3"
    };

    // Fábrica estática = método de classe que cria instâncias prontas
    // Igual a um factory function no JS: createFoodTimer()
    public static TimerConfig CreateFood() => new()
    {
        Name = "Food",
        Category = TimerCategory.Food,
        Duration = TimeSpan.FromMinutes(15),
        IsLooping = false
    };

    public static TimerConfig CreateBoost() => new()
    {
        Name = "Boost",
        Category = TimerCategory.Boost,
        Duration = TimeSpan.FromMinutes(60),
        IsLooping = true
    };

    public static TimerConfig CreatePotion() => new()
    {
        Name = "Potion",
        Category = TimerCategory.Potion,
        Duration = TimeSpan.FromMinutes(10),
        IsLooping = true
    };

}
