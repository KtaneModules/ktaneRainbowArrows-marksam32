using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using rnd = UnityEngine.Random;

public class ksmRainbowArrows : MonoBehaviour
{
    public KMBombInfo bombInfo;
    public KMAudio bombAudio;
    public KMBombModule bombModule;
    public Material[] colorMeshes;
    public KMSelectable[] arrowButtons;
    public TextMesh display;

    private readonly string[] __positionText = new string[]
    {
        "north",
        "northeast",
        "east",
        "southeast",
        "south",
        "southwest",
        "west",
        "northwest"
    };

    private readonly int[] __uniquenessOrderCCW = new int[] {0, 7, 1, 6, 2, 5, 3, 4};

    private readonly int[] __uniquenessOrderCW = new int[] {0, 1, 7, 2, 6, 3, 5, 4};

    private bool ccwRainbow;
    private int[] correctSequence;
    private Coroutine currentCoroutine;
    private int displayedDigits;
    private bool moduleSolved;
    private int positionInSequence;

    private float startColor;

    private static int globalLogID;
    private int thisLogID;

    private int whiteLocation;

    private void Awake()
    {
        thisLogID = ++globalLogID;
        for (int i = 0; i < arrowButtons.Length; i++)
        {
            int j = i;
            arrowButtons[i].OnInteract += () =>
            {
                ButtonPressed(j);
                return false;
            };
        }

        RandomizeArrows();
        display.text = string.Empty;
        bombModule.OnActivate += GenerateSolution;
    }

    private IEnumerator SolveAnimation()
    {
        var i = 0;
        yield return null;
        if (bombInfo.GetTime() < 7f)
        {
            while (i < 10)
            {
                display.text = rnd.Range(0, 10).ToString("\\G0");
                display.color = Color.HSVToRGB(startColor, (10 - i) * 0.1f, 1f);
                yield return new WaitForSeconds(0.025f);
                i++;
            }
        }
        else
        {
            while (i < 50)
            {
                display.text = rnd.Range(0, 100).ToString("00");
                display.color = Color.HSVToRGB((startColor + i * 0.01f) % 1f, 1f, 1f);
                yield return new WaitForSeconds(0.025f);
                i++;
            }

            while (i < 80)
            {
                display.text = rnd.Range(0, 10).ToString("\\G0");
                display.color = Color.HSVToRGB((startColor + i * 0.01f) % 1f, 1f, 1f);
                yield return new WaitForSeconds(0.025f);
                i++;
            }

            while (i < 100)
            {
                display.text = rnd.Range(0, 10).ToString("\\G0");
                display.color = Color.HSVToRGB((startColor + i * 0.01f) % 1f, (100 - i) * 0.05f, 1f);
                yield return new WaitForSeconds(0.025f);
                i++;
            }
        }

        display.text = "GG";
        display.color = new Color(1f, 1f, 1f);
        bombModule.HandlePass();
        currentCoroutine = null;
    }

    private IEnumerator StartupAnimation()
    {
        display.text = string.Empty;
        yield return new WaitForSeconds(0.5f);
        display.text = (displayedDigits / 10).ToString("0");
        yield return new WaitForSeconds(0.5f);
        display.text = displayedDigits.ToString("00");
    }

    private int RuleMazeNavigation(int pos)
    {
        var array = new[,]
        {
            {0, 20, 31, 16, 18, 27, 11, 12, 26, 9},
            {20, 0, 19, 14, 12, 15, 13, 10, 14, 13},
            {31, 19, 0, 25, 23, 6, 24, 21, 9, 24},
            {16, 14, 25, 0, 8, 21, 9, 12, 20, 9},
            {18, 12, 23, 8, 0, 19, 11, 10, 18, 11},
            {27, 15, 6, 21, 19, 0, 20, 17, 5, 20},
            {11, 13, 24, 9, 11, 20, 0, 9, 19, 4},
            {12, 10, 21, 12, 10, 17, 9, 0, 16, 7},
            {26, 14, 9, 20, 18, 5, 19, 16, 0, 19},
            {9, 13, 24, 9, 11, 20, 4, 7, 19, 0}
        };
        var num = displayedDigits % 10;
        var num2 = bombInfo.GetSerialNumberNumbers().LastOrDefault();
        var num3 = array[num, num2];
        var array2 = new[,]
        {
            {3, 7, 7, 7, 7, 7, 7, 7, 7, 7},
            {2, 7, 2, 2, 2, 2, 2, 2, 2, 2},
            {4, 4, 1, 4, 4, 4, 4, 4, 4, 4},
            {4, 3, 3, 7, 3, 3, 4, 3, 3, 4},
            {1, 1, 1, 1, 7, 1, 1, 1, 1, 1},
            {4, 4, 5, 4, 4, 1, 4, 4, 4, 4},
            {2, 2, 2, 2, 2, 2, 5, 2, 2, 2},
            {6, 7, 7, 7, 7, 7, 6, 3, 7, 6},
            {7, 7, 7, 7, 7, 7, 7, 7, 3, 7},
            {1, 0, 0, 0, 0, 0, 0, 1, 0, 5}
        };
        var num4 = array2[num, num2];
        if (num3 == 0)
        {
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (North - Maze Navigation) Starting position ({1}) and ending position ({2}) are the same.",
                thisLogID, num, num2);
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (North - Maze Navigation) Press the diagonal arrow corresponding to the relative location of square {1}; the answer is {2}.",
                thisLogID, num, __positionText[num4]);
        }
        else
        {
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (North - Maze Navigation) Navigation from square {1} to square {2} takes {3} steps.",
                thisLogID, num, num2, num3);
            Debug.LogFormat(
                (num4 & 1) == 1
                    ? "[Rainbow Arrows #{0}] (North - Maze Navigation) The first two moves are along different axes. Combine the two arrows and start from that arrow, which is {1}."
                    : "[Rainbow Arrows #{0}] (North - Maze Navigation) The first two moves are along the same axis. Start from the first move, which is {1}.",
                thisLogID, __positionText[num4]);
            num3 >>= 1;
            num4 = (num4 + num3) % 8;
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (North - Maze Navigation) Move {1} steps clockwise to reach the target direction; the answer is {2}.",
                thisLogID, num3, __positionText[num4]);
        }

        return num4;
    }

    private int RuleArrowDirection(int pos)
    {
        var num = displayedDigits * 4 % 360;
        var num2 = (whiteLocation + (int) Math.Round((num / 45f))) % 8;
        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (Northeast - Arrow Direction) Rotating {1} degrees from the white arrow brings us closest to the {2} arrow.",
            thisLogID, num, __positionText[num2]);
        if (pos % 4 == 3)
        {
            num2 = (num2 + 4) % 8;
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (Northeast - Arrow Direction) ... but we want the arrow directly opposite of that one; the answer is {1}.",
                thisLogID, __positionText[num2]);
        }
        else
        {
            Debug.LogFormat("[Rainbow Arrows #{0}] (Northeast - Arrow Direction) The answer is {1}.", thisLogID,
                __positionText[num2]);
        }

        return num2;
    }

    private int RuleBinaryRotation(int pos)
    {
        var num = 0U;
        num |= ((!correctSequence.Any((csn) => csn != -1 && csn % 2 == 1)) ? 0U : 1U);
        num |= ((!bombInfo.IsPortPresent(Port.Parallel) && !bombInfo.IsPortPresent(Port.Serial)) ? 0U : 2U);
        num |= ((displayedDigits % (bombInfo.GetBatteryCount() + 1) != 0) ? 0U : 4U);
        num |= ((bombInfo.GetBatteryHolderCount() < bombInfo.GetPortPlateCount()) ? 0U : 8U);
        Debug.LogFormat("[Rainbow Arrows #{0}] (East - Binary Rotation) Base four bit number is 0b{2}, or {1}.",
            thisLogID, num, Convert.ToString((num), 2).PadLeft(4, '0'));
        var num2 = bombInfo.GetIndicators().Count() % 4;
        if (num2 != 0)
        {
            num = (num >> num2 | num << 4 - num2);
        }

        Debug.LogFormat("[Rainbow Arrows #{0}] (East - Binary Rotation) Rotate right by {3} gives 0b{2}, or {1}.",
            thisLogID, num & 15U, Convert.ToString(((num & 15U)), 2).PadLeft(4, '0'), num2);
        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (East - Binary Rotation) Moving {1} step{3} from North gives an answer of {2}.",
            thisLogID, (int) (num & 15U), __positionText[(int) (num & 7U)], ((num & 15U) != 1U) ? "s" : string.Empty);
        return (int) (num & 7U);
    }

    private int RulePreviousArrows(int pos)
    {
        if (pos == 0)
        {
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (Southeast - Previous) This is the first rule, so the answer is the white arrow; that's {1}.",
                thisLogID, __positionText[whiteLocation]);
            return whiteLocation;
        }

        var num = correctSequence[pos - 1];
        while (Array.IndexOf(correctSequence, num) != -1)
        {
            num = (num + 7) % 8;
        }

        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (Southeast - Previous) Moving {3} from {2}, the first arrow not pressed is {1}.",
            thisLogID, __positionText[num], __positionText[correctSequence[pos - 1]], "counter-clockwise");
        num = (num + pos) % 8;
        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (Southeast - Previous) Moving {2} step{4} {3} from that position leads to an answer of {1}.",
            thisLogID, __positionText[num], pos, "clockwise", (pos != 1) ? "s" : string.Empty);
        return num;
    }

    private int RuleLetterString(int pos)
    {
        var text = string.Empty;
        foreach (char c in from ch in bombInfo.GetSerialNumber().Distinct() where ch >= 'A' && ch <= 'H' select ch)
            text += c;

        for (int i = 0; i < 8; i++)
        {
            if (!text.Contains("ABCDEFGH"[i]))
                text += "ABCDEFGH"[i];
        }

        Debug.LogFormat("[Rainbow Arrows #{0}] (South - Letter String) After modification 1, the string is \"{1}\".",
            thisLogID, text);
        if (bombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 == 0)
        {
            var num = bombInfo.GetSerialNumberNumbers().FirstOrDefault() % 8;
            if (num != 0)
            {
                var text2 = text.Substring(8 - num);
                text2 += text.Substring(0, 8 - num);
                text = text2;
            }

            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (South - Letter String) After modification 2, the string is \"{1}\".", thisLogID,
                text);
        }

        if (bombInfo.GetBatteryCount() % 2 == 1)
        {
            var str = string.Concat(string.Empty, text[0], text[2], text[4], text[6]);
            var str2 = string.Concat(string.Empty, text[1], text[3], text[5], text[7]);
            text = str + str2;
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (South - Letter String) After modification 3, the string is \"{1}\".", thisLogID,
                text);
        }

        if (whiteLocation == 0 || whiteLocation == 4)
        {
            text = new string(text.Reverse().ToArray());
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (South - Letter String) After modification 4, the string is \"{1}\".", thisLogID,
                text);
        }

        var num2 = (displayedDigits != 0) ? ((displayedDigits - 1) % 9 + 1) : 0;
        if (--num2 <= 0)
        {
            num2 = 1;
        }

        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (South - Letter String) We want character {1} in the final string, and that's {2}.",
            thisLogID, num2, text[num2 - 1]);
        var num3 = whiteLocation + (text[num2 - 1] - 'A');
        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (South - Letter String) The assigned arrow that corresponds to letter {2} is {1}.",
            thisLogID, __positionText[num3 % 8], text[num2 - 1]);
        return num3 % 8;
    }

    private int RuleHeadCount(int pos)
    {
        var source = bombInfo.GetSerialNumber().ToLower().Distinct().ToArray();
        var moduleNames = bombInfo.GetModuleNames();
        var array = new[]
        {
            "red",
            "orange",
            "yellow",
            "green",
            "blue",
            "indigo",
            "purple",
            "double"
        };
        var num = 1;
        var num2 = -1;
        for (var i = 0; i < 8; i++)
        {
            var color = array[i];
            var count = moduleNames.FindAll((mod) => mod.ToLower() == string.Format("{0} arrows", color)).Count;
            var num3 = 0;
            foreach (char value in color)
            {
                if (source.Contains(value))
                    num3++;
            }

            if (num2 < count + num3)
            {
                num2 = count + num3;
                num = i + 1;
            }

            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (Southwest - Head Count) For the word \"{1}\", {2} serial number matches + {3} module matches = value of {4}.",
                thisLogID, color, num3, count, count + num3);
        }

        var num4 = (num * ((!ccwRainbow) ? 1 : 7) + whiteLocation) % 8;
        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (Southwest - Head Count) The best value obtained was from the word \"{1}\", so move {2} step{3} from the white arrow; the answer is {4}.",
            thisLogID, array[num - 1], num, (num != 1) ? "s" : string.Empty, __positionText[num4]);
        return num4;
    }

    private int RuleAdjacentWords(int pos)
    {
        var array = new[]
        {
            "yoked",
            "white",
            "poets",
            "xysti",
            "lower",
            "tango",
            "magic",
            "joust",
            "farce",
            "along",
            "quirk",
            "hotel",
            "zeros",
            "royal",
            "bravo",
            "vault"
        };
        var array2 = new[]
        {
            "234",
            "23456",
            "23456",
            "456",
            "456",
            "01234",
            "01234567",
            "01234567",
            "04567",
            "01234",
            "01234567",
            "01234567",
            "04567",
            "012",
            "01267",
            "01267",
            "067"
        };
        var array3 = new[]
        {
            -4,
            -3,
            1,
            5,
            4,
            3,
            -1,
            -5
        };
        var num = 0;
        var num2 = 0;
        var second = bombInfo.GetSerialNumber().ToLower().Distinct().ToArray();
        for (var i = 0; i < array.Length; i++)
        {
            var num3 = array[i].ToCharArray().Intersect(second).ToArray().Length;
            if (num3 > num2)
            {
                num2 = num3;
                num = i;
            }
        }

        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (West - Adjacent Words) The word with the most letters in common with the serial number is \"{1}\", with {2}.",
            thisLogID, array[num], num2);
        var text = "abcdefghijklmnopqrstuvwxyz";
        if (displayedDigits % 26 != 0)
        {
            text = "abcdefghijklmnopqrstuvwxyz".Substring(displayedDigits % 26);
            text += "abcdefghijklmnopqrstuvwxyz".Substring(0, displayedDigits % 26);
        }

        Debug.LogFormat("[Rainbow Arrows #{0}] (West - Adjacent Words) The starting letter is '{1}'.", thisLogID,
            text[0]);
        var num4 = 0;
        var num5 = 27;
        var num6 = 0;
        foreach (var c in array2[num])
        {
            int num7 = (c - '0');
            int num8 = text.IndexOf(array[num + array3[num7]][0]);
            if (num8 < num5)
            {
                num5 = num8;
                num4 = num7;
                num6 = num + array3[num7];
            }
        }

        Debug.LogFormat(
            "[Rainbow Arrows #{0}] (West - Adjacent Words) The letter '{2}' gives us a match in the {3} direction ('{1}'), so the answer is {3}.",
            thisLogID, array[num6], array[num6][0], __positionText[num4]);
        return num4;
    }

    private int RuleBasicAppearance(int pos)
    {
        var array = bombInfo.GetSerialNumberNumbers().ToArray();
        var num = ((Array.IndexOf(array, displayedDigits / 10) == -1) ? 0 : 2) |
                  ((Array.IndexOf(array, displayedDigits % 10) == -1) ? 0 : 1);
        if (num == 3)
        {
            Debug.LogFormat("[Rainbow Arrows #{0}] (Northwest - Appearance) Both digits present. The answer is north.",
                thisLogID);
            return 0;
        }

        if (num == 2)
        {
            Debug.LogFormat("[Rainbow Arrows #{0}] (Northwest - Appearance) Left digit present. The answer is west.",
                thisLogID);
            return 6;
        }

        if (num != 1)
        {
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] (Northwest - Appearance) Neither digit present. The answer is south.",
                thisLogID);
            return 4;
        }

        Debug.LogFormat("[Rainbow Arrows #{0}] (Northwest - Appearance) Right digit present. The answer is east.",
            thisLogID);
        return 2;
    }

    private void ResetInput()
    {
        positionInSequence = 0;
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(StartupAnimation());
    }

    private void GenerateSolution()
    {
        positionInSequence = 0;
        displayedDigits = rnd.Range(0, 100);
        correctSequence = Enumerable.Repeat(-1, 8).ToArray();
        Debug.LogFormat("[Rainbow Arrows #{0}] The display shows \"{1:00}\".", thisLogID, displayedDigits);
        var list = new List<Func<int, int>>
        {
            RuleMazeNavigation,
            RuleArrowDirection,
            RuleBinaryRotation,
            RulePreviousArrows,
            RuleLetterString,
            RuleHeadCount,
            RuleAdjacentWords,
            RuleBasicAppearance
        };
        var num = whiteLocation;
        for (var i = 0; i < correctSequence.Length; i++)
        {
            Debug.LogFormat("[Rainbow Arrows #{0}] ----------", thisLogID);
            var num2 = list[num](i);
            var num3 = num2;
            for (var j = 0; j < 8; j++)
            {
                if (ccwRainbow)
                    num2 = num3 + __uniquenessOrderCCW[j];
                else
                    num2 = num3 + __uniquenessOrderCW[j];

                num2 %= 8;
                if (Array.IndexOf(correctSequence, num2) == -1)
                    break;
            }

            if (num3 != num2)
                Debug.LogFormat(
                    "[Rainbow Arrows #{0}] The above rule returned {2}, which wasn't unique. The closest unique arrow was {1}.",
                    thisLogID, __positionText[num2], __positionText[num3]);

            correctSequence[i] = num2;
            num = (num + ((!ccwRainbow) ? 1 : 7)) % 8;
        }

        startColor = rnd.Range(0f, 1f);
        display.color = Color.HSVToRGB(startColor, 1f, 1f);
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(StartupAnimation());
        var text = "The full correct sequence is: ";
        for (var k = 0; k < correctSequence.Length; k++)
            text = text + __positionText[correctSequence[k]] + ((k == 7) ? "." : ", ");

        Debug.LogFormat("[Rainbow Arrows #{0}] ----------", thisLogID);
        Debug.LogFormat("[Rainbow Arrows #{0}] {1}", thisLogID, text);
    }

    private void RandomizeArrows()
    {
        var num = rnd.Range(0, 16);
        ccwRainbow = ((num & 8) == 8);
        whiteLocation = (num & 7);
        Debug.LogFormat("[Rainbow Arrows #{0}] The white arrow is facing {1}, and the rainbow direction is {2}.",
            thisLogID, __positionText[whiteLocation], (!ccwRainbow) ? "clockwise" : "counter-clockwise");
        var num2 = whiteLocation;
        for (var i = 0; i < 8; i++)
        {
            num2 = (num2 + ((!ccwRainbow) ? 1 : 7)) % 8;
            arrowButtons[num2].GetComponent<Renderer>().material = colorMeshes[i];
        }
    }

    private void ButtonPressed(int button)
    {
        arrowButtons[button].AddInteractionPunch(0.25f);
        bombAudio.PlayGameSoundAtTransform(0, transform);
        if (moduleSolved || correctSequence == null)
            return;

        if (button != correctSequence[positionInSequence])
        {
            Debug.LogFormat(
                "[Rainbow Arrows #{0}] STRIKE: For input #{1}, you pressed {2} when I expected {3}. Input reset.",
                thisLogID, positionInSequence + 1, __positionText[button],
                __positionText[correctSequence[positionInSequence]]);
            bombModule.HandleStrike();
            ResetInput();
            return;
        }

        if (++positionInSequence >= correctSequence.Length)
        {
            Debug.LogFormat("[Rainbow Arrows #{0}] SOLVE: Button sequence has been input correctly.", thisLogID);
            moduleSolved = true;
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);

            currentCoroutine = StartCoroutine(SolveAnimation());
        }
    }

#pragma warning disable 414
    private const string TwitchHelpMessage =
        "Press arrows with '!{0} press UL R D', '!{0} press NW E S', or '!{0} press 7 6 2' (keypad order—8 is North). The word 'press' is optional, but spacing is important.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        var cmds = command.Split(' ').ToList();
        var presses = new List<int>();
        if (cmds.Count > 9)
            yield break;

        for (var i = 0; i < cmds.Count; i++)
        {
            if (Regex.IsMatch(cmds[i], "^(press|select)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                if (i != 0)
                {
                    yield break;
                }
            }
            else if (Regex.IsMatch(cmds[i], "^(?:U|T|TM|up|top|N|north|8)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(0);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:[UT]R|(?:up|top)-?right|NE|north-?east|9)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(1);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:MR|R|right|E|east|6)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(2);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:[DB]R|(?:down|bottom)-?right|SE|south-?east|3)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(3);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:D|B|BM|down|bottom|S|south|2)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(4);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:[DB]L|(?:down|bottom)-?left|SW|south-?west|1)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(5);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:ML|L|left|W|west|4)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(6);
            }
            else if (Regex.IsMatch(cmds[i], "^(?:[UT]L|(?:up|top)-?left|NW|north-?west|7)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                presses.Add(7);
            }
            else
            {
                if (Regex.IsMatch(cmds[i], "^(?:M|MM|middle|middlemiddle|C|center|5)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    yield return "sendtochaterror Rainbows don't have a center...";
                    yield break;
                }

                yield return string.Format(
                    "sendtochaterror I'm looking for a direction, what the heck is this '{0}' nonsense you gave me?",
                    cmds[i]);
                yield break;
            }
        }

        if (presses.Count > 0)
        {
            yield return null;
            if (presses.Count == 1)
            {
                yield return new[]
                {
                    arrowButtons[presses[0]]
                };
            }
            else
            {
                for (int j = 0; j < presses.Count; j++)
                {
                    yield return string.Format("strikemessage pressing {0} (input #{1})", __positionText[presses[j]],
                        j + 1);
                    yield return new[]
                    {
                        arrowButtons[presses[j]]
                    };
                }
            }

            if (moduleSolved)
            {
                yield return "solve";
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (moduleSolved)
        {
            yield break;
        }

        Debug.LogFormat("[Rainbow Arrows #{0}] Force solve requested by Twitch Plays.",
            thisLogID);
        while (!moduleSolved)
        {
            arrowButtons[correctSequence[positionInSequence]].OnInteract.Invoke();
            yield return new WaitForSeconds(0.125f);
        }

        while (currentCoroutine != null)
        {
            yield return true;
        }
    }
}