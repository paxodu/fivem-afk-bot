using System;

namespace Raides
{
    static class GlyphData
    {
        public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const int Fonts = 4;
        public const int Side = 24;
        public static readonly string Base64 =
        "AP8AAP8AAP8AAf+AAf+AAf/AA+/AA+fAA+fgB+fgB+fgD8PwD8PwD8P4H4H4H//4H//8P//8P//8PwB+fgB+fgB+/gA//AA///+A" +
        "///w///4///8/AP+/AD+/AD+/AD+/AD+/AH8///w///g///w///8/AH+/AB//AB//AB//AB//AD////+///+///8///gAP/AA//g" +
        "B//4D//8H//+P4B+PwA/fgA/fgAffgAA/AAA/AAA/AAA/AAAfAAAfgAAfgA/fwA/PwB/H+H+H//8D//4A//wAP/A//4A//+A///g" +
        "///w/A/8/AH8/AD+/AB+/AB//AA//AA//AA//AA//AA//AA//AB//AB+/AB+/AD+/AP8///4///w///g//8A////////////////" +
        "/gAA/gAA/gAA/gAA/gAA/gAA///+///+///+///+/gAA/gAA/gAA/gAA/gAA/gAA/////////////////////////////////wAA" +
        "/wAA/wAA/wAA/wAA/wAA/wAA///+///+///+///+/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAAAP+AA//gB//wH//4P//8P4D+" +
        "fwB+fgA//AAA/AAA/AAA/Af//Af//Af//Af//AA/fgA/fgA/fwB+P8H8H//8D//4B//wAf/A/AA//AA//AA//AA//AA//AA//AA/" +
        "/AA//AA//AA//////////////////AA//AA//AA//AA//AA//AA//AA//AA//AA//AA/////////////////////////////////" +
        "////////////////////////////////////////////////////////////////AAB/AAB/AAB/AAB/AAB/AAB/AAB/AAB/AAB/" +
        "AAB/AAB/AAB/AAB/AAB/AAB//wB//wB//wB/fwB/f4D/P//+P//+H//4A//g/AD+/AH8/AH8/AP4/Afw/A/g/B/A/D+A/H8A/P4A" +
        "/fwA//wA//4A//4A/78A/x/A/g/A/A/g/Afw/AP4/AP4/AH8/AD+/AB//wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA" +
        "/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/////////////////gB//gB//wD//wD//wD//4D//4H//8Hv/8Hv+8Pv+8PP++PP" +
        "++PP+efP+eeP+feP+PeP+P8P+P8P+H8P+H4P+H4P+D4P+DwP/gA//gA//wA//4A//8A//+A//+A///A///A//fg//Pw//H4//H4/" +
        "/D8//B+//A/f/A/f/Af//Af//AP//AH//AD//AD//AA/AP+AA//gB//wH//4P//8P4D+fwB+fgA//AA//AAf/AAf/AAf/AAf/AAf" +
        "/AAf/AA/fgA/fgA+fwB+P8H+H//8D//4B//wAf/A///A///w///4///8/gH+/gD//gB//gB//gB//gB//gD//gH+///+///8///4" +
        "///A/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAAAP+AA//gD//4H//8P//8PwD+fgA+fAA//AAf/AAf/AAf/AAf/AAf/AAf/AA/fh8/" +
        "fg/+P4f+P//8H//4D//4A//8AH8+AAA///+A///g///4///8/AP8/AD+/AD+/AD+/AD+/AD+/AH+/gf8///8///4///g///A/A/g" +
        "/A/g/Afw/AP4/AH8/AH8/AD+/AB/Af/AB//4H//8P//+P8H+fwB/fwA/fwAAfwAAP+AAP/+AH//wB//8AP/+AAP+AAB/AAA//gA/" +
        "/gA/f4D/f//+P//+H//8A//g////////////////AH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4A" +
        "AH4AAH4AAH4AAH4AAH4A/AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//gA//gA//gB//wB/f4H+" +
        "P//8H//8D//4A//A/AA//AA/fgA/fgB+fgB+PwB+PwD8P4D8H4D8H4H4D8H4D8PwD8PwB+PwB+PgA+fgA+fgA/fAAf/AAf+AAf+A" +
        "AP+AAP8AAH8A8DwP8DwP+DwPeD4OeH4eeH4eeH4eOH4ePH4cPH88PPc8POc8HOc8HOc4Hue4HuO4H+P4D8P4D8PwD8PwD8PwD8Hw" +
        "B4HwB4HwfgB+PwB+P4D8H4H8D8H4B+PwB+fgA/fgAf/AAf+AAP8AAH8AAH8AAP+AAf+AA//AA/fgB+fwD+PwH8H4H4D8PwD+fwB+" +
        "/gA//gA/fgB+fwB+PwD+H4D8H4H4D8P4D+PwB+fgA/fgA//AAf+AAP+AAP8AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4A" +
        "////////////////AAD+AAH8AAP4AAfwAA/gAB/AAD+AAH+AAP8AAP4AAfwAB/gAB/AAD+AAH8AAP4AAf///////////////AH4A" +
        "AH8AAP8AAP8AAP+AAf+AAffAA+fAA+fAA+PgB8PgB8PgD4HwD4HwD//4H//4H//4H//8P//8PgB+fgB+fgA+fAA//AAf///A///w" +
        "///8///+/AH+/AB+/AB//AB//AB+/AD+///8///w///w///8/AH+/AB//AB//AB//AB//AB////////+///8///gAP/AA//wB//4" +
        "D//8H//+P4B+PwA/fgA/fgAAfgAAfgAAfgAA/gAAfgAAfgAAfgAAfgA/PwA/P4B/H8H+H//+D//8B//4Af/g//4A///A///g///4" +
        "/A/8/AH+/AD+/AB//AA//AA//AA//AA//AA//AA//AA//AB//AB//AD+/AH+/A/8///4///w///A//4A////////////////////" +
        "/gAA/gAA/gAA/gAA/gAA///8///8///8///8/gAA/gAA/gAA/gAA/gAA/gAA/////////////////////////////////////gAA" +
        "/gAA/gAA/gAA/gAA///8///8///8///8///8/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAAAP/AA//wB//4D//8H//+P4B+PwA/" +
        "fgA/fgAAfgAAfgAAfgP//gf/fgf/fgf/fgA/fgA/PwA/P4B/H8D/H///D///B//vAf/P/AA//AA//AA//AA//AA//AA//AA//AA/" +
        "/AA//AA//////////////////AA//AA//AA//AA//AA//AA//AA//AA//AA//AA/////////////////////////////////////" +
        "////////////////////////////////////////////////////////////AAP/AAP/AAP/AAP/AAP/AAP/AAP/AAP/AAP/AAP/" +
        "AAP/AAP/AAP/AAP/AAP/AAP/AAP/AAP/AB/////////////+///8///w/gD+/gD8/gP8/gP4/gfw/g/g/h/A/j+A/n8A/v4A//4A" +
        "//8A//+A/9+A/5/A/w/g/gfg/gfw/gP4/gH8/gD8/gD+/gB+/gA//wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA" +
        "/wAA/wAA/wAA/wAA/wAA/wAA/wAA/wAA/////////////////gB//gB//wB//wD//wD//wD//4H//4H//8Hv+8Hv+8Pv++PP+efP" +
        "+eeP+eeP+PeP+P8P+P8P+P8P+H4P+H4P+H4P+D4P+DwP/AA//gA//gA//wA//4A//8A//+A///A///A//fg//fw//H4//D4//D8/" +
        "/B+//A///A///Af//AP//AH//AD//AD//AA//AA/Af+AA//gD//wH//4H//8PwD+fgB+fgA//gA//AA//AA//AA//AA//AA//AA/" +
        "/AA/fgA/fgB+PwD+P4H8H//8D//4B//wAf/A//+A///w///4///8/gH+/gB//gA//gA//gA//gA//gB//gH+///8///4///g//8A" +
        "/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAAAf+AB//gD//4H//8P4D+fgB+fgA//gA//AA//AA//AA//AA//AA//AA/fgA/fgB+P4D+" +
        "H//8D//wB//wAf/8AAD/AAB/AAAf///g///w///8///+/AH+/AB//AB//AB//AB//AB//AD+///+///8///w///g/Afw/Afw/APw" +
        "/AH4/AH8/AD8/AD+/AB+/AB/Af/AD//4H//8P//+P4D+fgA/fgA/fgAAf4AAf/gAP//AH//4D//8A//+AB//AAH/AAA//AA//gA/" +
        "fwB/f+P+P//8H//4A//g////////////////////AH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8A" +
        "AH8AAH8AAH8AAH8A/gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA//gA/fwB/f4H/P//+" +
        "P//8H//4A//g/AAf/AA/fAA+fgA+PgB+PgB8PwB8HwD8H4D4H4H4D4HwD8HwB8PwB8PgB+PgA+fgA+fAAffAAf+AAf+AAP+AAP8A" +
        "AP8AAH4A8BwP8DwP8DwPcDwOeDweeD4eeH4eOH4ePH4cPH48PP88POc8HOc8Hue4H+f4H+P4D8P4D8PwD8PwD8HwD4HgB4HgB4Hg" +
        "B4HgfwB+PwB8P4D8H4H4D8HwD+PwB/fgA/fAA/+AAf+AAP8AAH4AAH8AAP+AAf+AAf/AA//gB+fwD8PwD4H4H4H8PwD8PgD+fgB/" +
        "/AA/fgA+PwB+PwD8H4D8D4H4D8PwB+PwA+fgA/fAAf/AAP+AAP8AAH8AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4Af///" +
        "f///f///f///P//+AAH8AAf4AAfwAA/gAB/AAD/AAH8AAP4AAP4AAfwAA/gAB/AAD+AAH8AAP4AAf///////////////AP8AAP8A" +
        "AP+AAf+AAf+AAf/AA//AA//gB+fgB+fgB+fgD8PwD8PwD8H4H4H4H//4H//8P//8P//+f//+fgB+fgB+/gB//AA////A///w///4" +
        "///8///+/wH+/wH+/wH+/wH+/wP8///8///w///8///+/wP+/wD//wD//wD//wD//wP////+///+///8///gAD/4AP/+B///D///" +
        "H///P/AHP8ABf4AA/4AA/wAA/wAA/wAA/wAA/wAA/wAA/wAAf4AAf8AAP+ADH/wfD///B///A///AH/4//4A///A///g///4///8" +
        "/gf+/gH+/gD//gB//gB//gB//gB//gB//gB//gB//gB//gD//gH+/gP+///8///4///w///g//8A/////////////////////4AA" +
        "/4AA/4AA/4AA///+///+///+///+///+/4AA/4AA/4AA/4AA/4AA/////////////////////////////////////////4AA/4AA" +
        "/4AA/4AA///+/////////////////4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AAAH/wAf/+B//+H//+P//+P+AOf4ACfwAA" +
        "/wAA/gAA/gAA/gP//gP//gP//gP//gA//wA/f4A/f8A/P/B/H///D///A//+AP/w/gB//gB//gB//gB//gB//gB//gB//gB//gB/" +
        "/////////////////////gB//gB//gB//gB//gB//gB//gB//gB//gB//gB/////////////////////////////////////////" +
        "////////////////////////////////////////////////////////AB//AB//AB//AB//AB//AB//AB//AB//AB//AB//AB//" +
        "AB//AB//AB//AB//AB//AB//AB//AD//AH/+///+///4///A//gA/gH+/gP8/gf4/g/w/h/g/j/A/n+A/v4A//wA//gA//AA//AA" +
        "//AA//gA//wA//4A/v8A/n/A/h/g/h/w/g/4/gf8/gP+/gD//4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA/4AA" +
        "/4AA/4AA/4AA/4AA/4AA/4AA/////////////////////gB//wD//wD//4D//4H//4H//8H/+8P/+8Pf++ff+eff+fef+P+f+P8f" +
        "+P8f+H8f+H4f+H4f+Dwf+Dwf+AAf+AAf+AAf+AAf/wB//4B//4B//8B//8B//+B///B///B///h//vh//vx//nx//n5//j9//h9/" +
        "/h///g///g///gf//gf//gP//gH//gH//gD/AP8AA//gD//wH//4P//8f4H+fwD+/gB+/gB//gB//gB//gA//gA//gB//gB//gB/" +
        "/gB+fwD+fwH+P+f8H//4D//wB//gAf+A///A///w///8///8///+/wH//wD//wD//wD//wD//wH////+///8///8///w///A/wAA" +
        "/wAA/wAA/wAA/wAA/wAA/wAA/wAAAP8AB//gD//4H//8P4H8fwD+/gB+/gB//gB//gA//gA//gB//gB//gB/fwD+fwH+P+f8H//4" +
        "D//wA//AAA/gAAPwAAHwAAD8//8A///A///g///w///w/gf4/gP4/gP4/gP4/gfw/g/w///g//+A//+A///A///g/g/w/gfw/gP4" +
        "/gP8/gH8/gH+/gD+/gD/A//gD//8H//8P//8f//8f4A8/wAA/gAA/4AAf/wAf//AP//4H//8A//+AH//AAH/AAD/AAD/YAD/fwP+" +
        "f//+f//8f//4B//A////////////////////AH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8AAH8A" +
        "AH8AAH8AAH8A/gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//gB//wD/f4D/f+P+P//8H//4" +
        "D//wA//A/AA//gA//gB/fgB+fgB+PwD+PwD8H4D8H4H4H4H4H8H4D8PwD+PwD+fwB+fgB+fgA//gA//AA//AAf+AAf+AAf+AAP8A" +
        "AP8A8D4P+D4P+D4PeH4feH4feH4efH8efH8efHcePPc+POc+POc+Puc8PuM8PuO8H+P8H8P8H8P4H8P4H8H4D8H4D8H4D4H4D4Hw" +
        "/gB/fwD+PwD+P4H8H8P4D+PwB+fgB//gA//AA//AAf+AAP8AAP+AAf+AA//AA//AB//gB+fwD8PwH8P4P4H8PwD+fwB+/gB//gB/" +
        "fwD+fwD+P4H8H4H8H8P4D+fwB+fwB//gA//AA//AAf+AAP8AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4Af///f///" +
        "f///f///f//+AAP8AAf4AA/wAB/wAD/gAD/AAH+AAP8AAf4AA/wAB/gAD/AAH+AAP8AAf///////////////////AH4AAP8AAP8A" +
        "AP8AAf+AAf+AA+fAA+fAA+fAB8PgB8PgB4HgD4HwD4HwH//4H//4H//4P//8PgB8PgB8fgB+fAA+/AA++AAf///A///w///4///8" +
        "/AH8/AD+/AD+/AD+/AD8/AH8///4///g///4///8/AB+/AA//AA//AA//AA//AB////+///+///8///gAP+AA//wD//4H//8H8H+" +
        "PwB+fgA/fgAYfAAA/AAA/AAA/AAA/AAA/AAA/AAAfAAAfgAYfgAfPwA/P4B+H//8D//8B//wAf/g//4A///A///w///4/A/8/AH8" +
        "/AD+/AB+/AB//AA//AA//AA//AA//AA//AA//AA//AB+/AB+/AD+/AH8///4///w///g//8A/////////////////AAA/AAA/AAA" +
        "/AAA/AAA/AAA///8///+///+///+/AAA/AAA/AAA/AAA/AAA/AAA/////////////////////////////////gAA/gAA/gAA/gAA" +
        "/gAA/gAA/gAA/gAA/////////////////gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAAAP/AA//wD//4H//8P8H+fwB+fgA+fAAQ/AAA" +
        "/AAA/AAA+AP/+Af//Af//Af//AAf/AAffgAffgAfPwA/P///H//+D//8Af/g/AA//AA//AA//AA//AA//AA//AA//AA//AA//AA/" +
        "/////////////////AA//AA//AA//AA//AA//AA//AA//AA//AA//AA/////////////////////////////////////////////" +
        "////////////////////////////////////////////////////AD//AD//AD//AD//AAB/AAB/AAB/AAB/AAB/AAB/AAB/AAB/" +
        "AAB/AAB/AAB/AAB/AAB//wB//wD/f4D/P//+P//+H//4A//g/AB+/AD8/AH4/APw/A/g/B/A/D+A/H8A/P4A/fwA//gA//wA//4A" +
        "/34A/j8A/B+A/B/A/A/g/Afg/APw/AH4/AD8/AD+/AB//gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA/gAA" +
        "/gAA/gAA/gAA/gAA/gAA/gAA/////////////////gB//gB//wB//wD//wD//4D//4H//4H//8Hv+8Hv+8Pv++PP+efP+efP+eeP" +
        "+PeP+P+P+P8P+H8P+H8P+H4P+D4P+D4P+DwP/gAf/wAf/4Af/4Af/8Af/+Af/+Af//Af/fgf/fgf/Pwf/Hwf/D4f/D8f/B+f/B+f" +
        "/A/f/Af//AP//AP//AH//AH//AD//AB/AP+AA//gD//wH//4P8P8PwD+fgB+fAA//AAf/AAf/AAf+AAf+AAf/AAf/AAf/AAf/AA/" +
        "fgA+fgB+PwD+H//8D//4B//wAf/A///A///4///8///+/AH+/AB//AA//AA//AA//AB//AB//AH+///8///4///g//8A/AAA/AAA" +
        "/AAA/AAA/AAA/AAA/AAA/AAAAP+AB//gH//4P8P8PwB+fgA//AA//AAf+AAf+AAf/AAf/AAf/AAffAA+fgB+P4H8H//4B//wAf+A" +
        "AB8AAB+AAB/8AA/8AAP8///A///w///4///8/AH+/AB+/AB+/AA+/AA+/AB+/AD+///8///4///g///A/A/g/Afg/APw/AP4/AH4" +
        "/AH8/AD8/AB+/AA/Af/AB//4H//8H//+PwD+PwB+fwA+PwAAP8AAP/gAH//AD//4A//8AH/+AAf/AAB/AAA/fAA//gA/fwA/f//+" +
        "P//+H//8A//g////////////////AH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4A" +
        "AH4AAH4A/AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA//AA/fgB/fgB+fwD+P//8H//8D//4" +
        "B//A/AAf/AA/fgA/fgA+PgB+PwB8PwB8HwD8H4D4D4D4D4H4D4HwB8HwB8PgA+PgA+PgA+fAAefAAffAAf+AAP+AAP8AAH8AAH8A" +
        "8DwP8D4P8D4PcD4OeH4OeH4OeH4eOH8eOH8eOGccOOccPOccHOc8HOO8HOO4HsO4HsO4H8P4D8P4D8P4D8HwD8HwD4HwD4HwfgA+" +
        "PwB+HwD8H4D4D4H4B8HwB+PgA+fgAf/AAf/AAP+AAH8AAP+AAf+AAf/AA/fgB+PgB8PwD8H4D4D4HwD8PwB8PgB+fAA//AA/fAA+" +
        "PgB+PwD8HwH4H4H4D8PwD8PwB+fgA+fAA//AAf+AAP8AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AAH4AP//+P//+P//+" +
        "P//+AAD+AAH8AAP4AAfwAA/gAB/AAD+AAH8AAP4AAfwAA/gAB/AAD+AAH8AAP4AAPwAA////////////////" ;

        public static byte[] Bytes()
        {
            return Convert.FromBase64String(Base64);
        }
    }
}
