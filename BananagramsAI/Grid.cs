﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BananagramsAI {
    class Grid {
        Dictionary<int, Line> rows = new Dictionary<int, Line>();
        Dictionary<int, Line> columns = new Dictionary<int, Line>();

        public Grid() { }

        public Grid(Grid grid) {
            this.rows = grid.rows.ToDictionary(pair => pair.Key, pair => new Line(pair.Value));
            this.columns = grid.columns.ToDictionary(pair => pair.Key, pair => new Line(pair.Value));
        }

        public int TopRowIndex {
            get {
                return rows.Keys.Min();
            }
        }

        public int BottomRowIndex {
            get {
                return rows.Keys.Max();
            }
        }

        public int LeftmostColumnIndex {
            get {
                return columns.Keys.Min();
            }
        }
        
        public void PlaceWordAt(string word, int x, int y, bool vertical) {
            int alongStart = (vertical ? y : x);
            int acrossStart = (vertical ? x : y);
            int acrossIndex = (vertical ? y : x);

            Dictionary<int, Line> alongAxis = (vertical ? columns : rows);
            int alongIndex = (vertical ? x : y);
            Line along = GetLineLazy(alongAxis, alongIndex);
            Dictionary<int, Line> across = (vertical ? rows : columns);
            
            for (int i = 0; i < word.Length; ++i) {
                along[alongStart + i] = word[i];

                if (!across.ContainsKey(acrossStart + i)) {
                    across[acrossStart + i] = new Line();
                }

                across[acrossStart + i][acrossIndex] = word[i];
            }
        }

        Line GetLineLazy(Dictionary<int, Line> axis, int index) {
            if (axis.TryGetValue(index, out Line line)) {
                return line;
            } else {
                axis[index] = new Line();
                return axis[index];
            }
        }

        public void Display() {
            int leftmost = rows.Values.Select(line => line.BackIndex).Min();

            foreach (int index in rows.Keys.OrderBy(y => y)) {
                Line row = rows[index];

                for (int j = leftmost; j < row.BackIndex; ++j) {
                    Console.Write(' ');
                }

                for (int i = row.BackIndex; i <= row.FrontIndex; ++i) {
                    if (row.TryGetTile(i, out char tile)) {
                        Console.Write(tile);
                    } else {
                        Console.Write(' ');
                    }
                }

                Console.WriteLine();
            }
        }

        public bool IsEmptyLine(int lineIndex, bool column) {
            return (column ? columns : rows)[lineIndex].IsEmpty();
        }
        
        string GenerateLineRegexFrom(int lineIndex, int start, bool column, out int last) {
            Line line;
            Line[] neighbors = new Line[2];
            if (column) {
                if (!columns.TryGetValue(lineIndex, out line)) throw new Exception();
                columns.TryGetValue(lineIndex - 1, out neighbors[0]);
                columns.TryGetValue(lineIndex + 1, out neighbors[1]);
            } else {
                if (!rows.TryGetValue(lineIndex, out line)) throw new Exception();
                rows.TryGetValue(lineIndex - 1, out neighbors[0]);
                rows.TryGetValue(lineIndex + 1, out neighbors[1]);
            }

            if (neighbors[0] == null) neighbors[0] = new Line();
            if (neighbors[1] == null) neighbors[1] = new Line();

            string anchor = String.Format("{0}", line[start]);

            if (!line.IsEmptyAt(start + 1)) { 
                while (!line.IsEmptyAt(++start)) {
                    anchor = anchor + line[start];
                }
            } else {
                start += 1;
            }

            last = start + 1;

            int next = -1;
            for (int i = start; i <= line.FrontIndex; ++i) {
                if (!(neighbors[0].IsEmptyAt(i) && neighbors[1].IsEmptyAt(i))) {
                    last = i + 1;
                    return String.Format("{0}(?<1>.{{0,{1}}})", anchor, i - start - 1);
                } else if (!line.IsEmptyAt(i)) {
                    next = i;
                    break;
                }
            }

            if (next == -1) {
                int upTo = int.MaxValue; 
                int end = Math.Max(neighbors[0].FrontIndex, neighbors[1].FrontIndex);
                for (int i = line.FrontIndex + 1; i <= end; ++i) {
                    if (!(neighbors[0].IsEmptyAt(i) && neighbors[1].IsEmptyAt(i))) {
                        upTo = i;
                        break;
                    } 
                }

                string suffix;
                if (upTo == int.MaxValue) {
                    suffix = "(?<1>.*)$";
                } else {
                    suffix = String.Format("(?<1>.{{0,{0}}})$", upTo - start);
                }

                return anchor + suffix;
            }
            
            int leeway = next - lineIndex - 1;

            string endEarly = String.Format("(?<1>.{{0,{0}}})$", leeway - 1);
            string fromNext = GenerateLineRegexFrom(lineIndex, next, column, out int whoCares);
            string goOn = String.Format("(?<1>.{{{0}}})({1})", leeway, fromNext);
            return String.Format("{0}(({1})({2}))", anchor, goOn, endEarly);
        }

        public Dictionary<int, Regex> GenerateLineRegexes(int lineIndex, bool column) {
            Dictionary<int, Line> axis = (column ? columns : rows);
            Line line = axis[lineIndex];
            Dictionary<int, Regex> regexes = new Dictionary<int, Regex>();

            Line[] neighbors = new Line[2];
            neighbors[0] = GetLineLazy(axis, lineIndex - 1);
            neighbors[1] = GetLineLazy(axis, lineIndex + 1);

            int start = Math.Min(neighbors[0].BackIndex, neighbors[1].BackIndex);
            int last = int.MinValue;
            for (int i = start; i <= line.FrontIndex; ++i) {
                if (!line.IsEmptyAt(i)) {
                    start = i;
                    break;
                } else if (!(neighbors[0].IsEmptyAt(i) && neighbors[1].IsEmptyAt(i))) {
                    last = i + 1;
                    start = last;
                }
            }

            for (int i = line.BackIndex; i <= line.FrontIndex; ++i) {
                if (!line.IsEmptyAt(i)) {
                    string prefix;
                    if (last == int.MinValue) {
                        prefix = "(?<1>^.*)";
                    } else { 
                        prefix = String.Format("(?<1>^.{{0,{0}}})", i - last);
                    }

                    string regexFrom = GenerateLineRegexFrom(lineIndex, i, column, out last);
                    string regexString = prefix + regexFrom;
                    Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                    regexes[i] = regex;
                    i = last - 1;
                }
            }

            return regexes;
        }  
    }
}
