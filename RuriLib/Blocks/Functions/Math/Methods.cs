using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;

namespace RuriLib.Blocks.Functions.MathFunctions
{
    [BlockCategory("Math Functions", "Blocks for mathematical operations", "#66cdaa")]
    public static class Methods
    {
        [Block("Returns the absolute value of a number")]
        public static float MathAbs(BotData data, float value)
        {
            var result = System.Math.Abs(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Abs({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Rounds a number to the nearest integer")]
        public static int MathRound(BotData data, float value)
        {
            var result = (int)System.Math.Round(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Round({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Rounds a number down to the nearest integer")]
        public static int MathFloor(BotData data, float value)
        {
            var result = (int)System.Math.Floor(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Floor({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Rounds a number up to the nearest integer")]
        public static int MathCeil(BotData data, float value)
        {
            var result = (int)System.Math.Ceiling(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Ceil({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the minimum of two values")]
        public static float MathMin(BotData data, float a, float b)
        {
            var result = System.Math.Min(a, b);
            data.Logger.LogHeader();
            data.Logger.Log($"Min({a}, {b}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the maximum of two values")]
        public static float MathMax(BotData data, float a, float b)
        {
            var result = System.Math.Max(a, b);
            data.Logger.LogHeader();
            data.Logger.Log($"Max({a}, {b}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Clamps a value between a minimum and maximum")]
        public static float MathClamp(BotData data, float value, float min, float max)
        {
            var result = System.Math.Clamp(value, min, max);
            data.Logger.LogHeader();
            data.Logger.Log($"Clamp({value}, {min}, {max}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Raises a number to the specified power")]
        public static float MathPow(BotData data, float baseValue, float exponent)
        {
            var result = (float)System.Math.Pow(baseValue, exponent);
            data.Logger.LogHeader();
            data.Logger.Log($"Pow({baseValue}, {exponent}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the square root of a number")]
        public static float MathSqrt(BotData data, float value)
        {
            var result = (float)System.Math.Sqrt(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Sqrt({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the remainder of dividing a by b")]
        public static int MathMod(BotData data, int a, int b)
        {
            var result = a % b;
            data.Logger.LogHeader();
            data.Logger.Log($"{a} % {b} = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the natural logarithm of a number")]
        public static float MathLog(BotData data, float value)
        {
            var result = (float)System.Math.Log(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Log({value}) = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Returns the base-10 logarithm of a number")]
        public static float MathLog10(BotData data, float value)
        {
            var result = (float)System.Math.Log10(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Log10({value}) = {result}", LogColors.YellowGreen);
            return result;
        }
    }
}
