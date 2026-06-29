namespace TimerApp.Core.Models;

// Enum = union type do TypeScript
// TS: type TimerCategory = "Food" | "Boost" | "Potion" | "Custom"

public enum TimerCategory
{
    Food, // 15 minutos
    Boost, // 60 minutos
    Potion, // 10 minutos
    Custom // configurável pelo usuário
}