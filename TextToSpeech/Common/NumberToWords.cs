
using System;

namespace GodotTTS.Speech
{
  class NumberToWords
  {
    private static string[] units = {"zero", "one", "two", "three",
    "four", "five", "six", "seven", "eight", "nine", "ten", "eleven",
    "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
    "seventeen", "eighteen", "nineteen"};
    private static string[] tens = {"", "", "twenty", "thirty", "forty",
    "fifty", "sixty", "seventy", "eighty", "ninety"};
    
    public static string ConvertAmount(double amount)
    {
        Int64 amount_int = (Int64)amount;
        Int64 amount_dec = (Int64)Math.Round((amount - (double)(amount_int)) * 100);
        if (amount_dec == 0)
        {
          return NumberToWord(amount_int);
        }
        else  
        {
          return NumberToWord(amount_int) + " point " + NumberToWord(amount_dec);
        }
    }
    
    public static string NumberToWord(Int64 i)
    {
      if (i < 20)
        return units[i];

      if (i < 100)
        return tens[i / 10] + ((i % 10 > 0) ? " " + NumberToWord(i % 10) : "");

      if (i < 1_000)
        return units[i / 100] + " hundred"  
            + ((i % 100 > 0) ? " and " + NumberToWord(i % 100) : "");

      if (i < 1_000_000)
        return NumberToWord(i / 1_000) + " thousand"
            + ((i % 1_000 > 0) ? ", " + NumberToWord(i % 1_000) : "");

      if (i < 1_000_000_000)
        return NumberToWord(i / 1_000_000) + " million"  
            + ((i % 1_000_000 > 0) ? ", " + NumberToWord(i % 1_000_000) : "");

      if (i < 1_000_000_000_000)
        return NumberToWord(i / 1_000_000_000) + " billion"  
            + ((i % 1_000_000_000 > 0) ? ", " + NumberToWord(i % 1_000_000_000) : "");

      return NumberToWord(i / 1_000_000_000_000) + " trillion"  
          + ((i % 1_000_000_000_000 > 0) ? ", " + NumberToWord(i % 1_000_000_000_000) : "");
    }
  }
}