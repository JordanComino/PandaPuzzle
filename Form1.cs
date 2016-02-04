using System;

using System.Text;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;

using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PandaPuzzle
{
	public partial class Form1 : Form
	{
		int mMaxWidth = 0;
		int mMaxHeight = 0;
		int mSquareSize = 30;
		int mDrawOffset = 10;

		List<SquareType> mBoard = new List<SquareType>();
		bool mBoardHasChanged = false;
		//List<List<int>> mRowRules = new List<List<int>>();
		//List<List<int>> mColRules = new List<List<int>>();

		//List<RuleSet> mRowRules = new List<RuleSet>();
		//List<RuleSet> mColRules = new List<RuleSet>();
		List<RuleSet> mRules = new List<RuleSet>();

		public Form1()
		{
			InitializeComponent();
			LoadData();
			try
			{
				Solve();
			}
			catch (Exception e)
			{
			}
		}

		private void Solve()
		{
			// Setup board
			CalculateMinMax();
			SolveOverlapRules();

			int count = 0;

			while (mBoardHasChanged)
			{
				while (mBoardHasChanged)
				{
					++count;
					mBoardHasChanged = false;
					BleedEdges();
					SolveOverlapRules();
				}

				AnalyzeRules();
				SolveOverlapRules();
				FinaliseSolvedRuleSets();
            }

			Console.WriteLine(count);
		}

		// Checks to see if any given rules fill up the whole line with the minimum possible whitespace (i.e., 1), and puts those answers onto our board
		private void SolveFullRules(List<RuleSet> rules)
		{
			foreach (RuleSet rule in rules)
			{
				int x, y;
				if (rule.mID == RuleID.RI_COLUMN)
				{
					x = rule.mIndex;
					y = 0;
				}
				else
				{
					x = 0;
					y = rule.mIndex;
				}

				int max = rule.mID == RuleID.RI_COLUMN ? mMaxHeight : mMaxWidth;
				if (MinSpaceRequired(rule) == max)
					SolveFullSet(rule, x, y);
			}
		}

		private void CalculateMinMax()
		{
			foreach (RuleSet rule in mRules)
				CalculateRuleSetMinMax(rule);
		}

		private void CalculateRuleSetMinMax(RuleSet rule)
		{
			CalculateRuleSetMin(rule);
			rule.mRules.Reverse();
			CalculateRuleSetMax(rule);
			rule.mRules.Reverse();
		}

		// Assumes that the RuleSet has already been put in order
		private void CalculateRuleSetMax(RuleSet rule)
		{
			int p = (rule.mID == RuleID.RI_COLUMN ? mMaxHeight : mMaxWidth) - 1;
			foreach (RuleValue value in rule.mRules)
			{
				p = value.SetMaxIndex(p);
				p -= value.mValue + 1;
			}
		}

		// Assumes that the RuleSet has already been put in order
		private void CalculateRuleSetMin(RuleSet rule)
		{
			int p = 0;
			foreach (RuleValue value in rule.mRules)
			{
				p = value.SetMinIndex(p);
				p += value.mValue + 1;
			}
		}

        private void SolveOverlapRules()
        {
			foreach (RuleSet rule in mRules)
			{
				if (rule.IsSolved())
					continue;

				foreach (RuleValue value in rule.mRules)
				{
					SolveOverlapValue(rule, value);
				}
			}
        }

		private void SolveOverlapValue(RuleSet rule, RuleValue value)
		{
			if (value.IsSolved())
				return;

			int range = (value.maxIndex - value.minIndex) + 1;
			if (range < value.mValue * 2) // we have some overlap, and therefore some guaranteed squares
			{
				int offset = range - value.mValue;
				for (int idx = offset; idx < value.mValue; ++idx)
				{
					if (rule.mID == RuleID.RI_ROW)
						SetBoardValue(idx + value.minIndex, rule.mIndex, SquareType.ST_BLACK);
					else
						SetBoardValue(rule.mIndex, idx + value.minIndex, SquareType.ST_BLACK);
				}
			}
		}

		private void BleedEdges()
		{
			foreach(RuleSet rule in mRules)
			{
				if (rule.IsSolved())
					continue;
				//if (rule.mID == RuleID.RI_COLUMN)
				//if (rule.mID == RuleID.RI_ROW)
				//	continue;

				Direction dir = (rule.mID == RuleID.RI_ROW ? Direction.RIGHT : Direction.DOWN);
				foreach (RuleValue value in rule.mRules)
				{
					if (value.IsSolved())
						continue;

					CalculateRuleSetMin(rule);
					if (BleedValue(rule, value, dir) == false)
						break;
				}
				rule.mRules.Reverse();

				dir = (rule.mID == RuleID.RI_ROW ? Direction.LEFT : Direction.UP);
				foreach (RuleValue value in rule.mRules)
				{
					if (value.IsSolved())
						continue;

					CalculateRuleSetMax(rule);
					if (BleedValue(rule, value, dir) == false)
						break;
				}
				rule.mRules.Reverse();
			}
		}

		private bool BleedValue(RuleSet rule, RuleValue value, Direction direction)
		{
			int x, y;
			if(rule.mID == RuleID.RI_ROW)
			{
				x = direction == Direction.RIGHT ? value.minIndex : value.maxIndex;
				y = rule.mIndex;
			}
			else
			{
				x = rule.mIndex;
				y = direction == Direction.DOWN ? value.minIndex : value.maxIndex;
			}
			int idx = GetIndex(x, y);

			// Have we already found our start position?
			if (mBoard[idx] == SquareType.ST_BLACK)
			{
				bool canContinue = false;
				for (int i = 0; i < value.mValue; ++i)
				{
					SetBoardValue(idx, SquareType.ST_BLACK);
					value.AddIndex(idx);
					canContinue = ShiftIndex(ref idx, direction);
				}
				if (canContinue)
					SetBoardValue(idx, SquareType.ST_WHITE);

				value.ClampMinMax(rule.GetBleedValue(x, y), value.mValue - 1, direction);

				return true;
			}

			bool foundPortion = false;
			bool isValid = false;
			for (int i = 0; i < value.mValue; ++i)
			{
				if (mBoard[idx] == SquareType.ST_UNKNOWN)
				{
					if (foundPortion) // If we already found a portion of ourselves, it's impossible for this to not be black
					{
						SetBoardValue(idx, SquareType.ST_BLACK);
						//if (direction == Direction.RIGHT || direction == Direction.DOWN)
						//value.ClampMinMax(i + (value.mValue - 1));
						value.ClampMinMax(rule.GetBleedValue(x, y),  i + (value.mValue - 1), direction);
						//else
						//	value.ClampMinMax(value.maxIndex - i - (value.mValue - 1));
					}
					isValid = ShiftIndex(ref idx, direction);

					continue;
				}

				if (mBoard[idx] == SquareType.ST_WHITE) // It's not possible to fit in this space. Blank it out, and move ourselves forward
				{
					Debug.Assert(foundPortion == false); // It shouldn't be possible to have found a portion of ourselves, and then run out of room.

					int initIdx = GetIndex(x, y);
					for (int j = 0; j < i; ++j)
					{
						SetBoardValue(initIdx, SquareType.ST_WHITE);
						ShiftIndex(ref initIdx, direction);
					}

					if (direction == Direction.RIGHT || direction == Direction.DOWN)
						value.minIndex += (i + 1);
					else
						value.maxIndex -= (i+1);

					return BleedValue(rule, value, direction);
				}

				if (mBoard[idx] == SquareType.ST_BLACK)
				{
					if (!foundPortion) // special case. We can sometimes reduce our max range by 1 if this would make two black runs collide
					{
						int p;
						if (GetIndexOffset(idx, value.mValue, direction, rule, out p))
						{
							if (mBoard[p] == SquareType.ST_BLACK)
							{
								value.ClampMinMax(rule.GetBleedValue(x, y), i + (value.mValue - 2), direction);
							}
						}
					}

					foundPortion = true;
					//if (direction == Direction.RIGHT || direction == Direction.DOWN)
					//	value.SetMaxIndex(value.minIndex + i + (value.mValue - 1));
					//else
					//	value.SetMinIndex(value.maxIndex - i - (value.mValue - 1));
					value.ClampMinMax(rule.GetBleedValue(x, y), i + (value.mValue - 1), direction);
				}

				isValid = ShiftIndex(ref idx, direction);
			}

			if (isValid)
			{
				if (foundPortion && mBoard[idx] == SquareType.ST_WHITE)
				{
					SetBoardValue(x, y, SquareType.ST_BLACK);
					BleedValue(rule, value, direction);
				}
				else if (mBoard[idx] == SquareType.ST_BLACK)
				{
					int initIdx = GetIndex(x, y);
					while (isValid && mBoard[idx] == SquareType.ST_BLACK)
					{
						SetBoardValue(initIdx, SquareType.ST_WHITE);
						if (direction == Direction.RIGHT || direction == Direction.DOWN)
							++value.minIndex;
						else
							--value.maxIndex;

						ShiftIndex(ref initIdx, direction);
						isValid = ShiftIndex(ref idx, direction);
					}
				}
			}

			return false;
		}

		// Here we look for all the special case rules for sequence placement, as well as going beyond just the edges.
		private void AnalyzeRules()
		{
			foreach (RuleSet rule in mRules)
			{
				if (rule.IsSolved())
					continue;

				ClampToLargerRun(rule);

				foreach (RuleValue value in rule.mRules)
				{
					if (value.IsSolved())
						continue;

					RedeuceValuesMinMax(rule, value);

					
				}
			}
		}

		private void FinaliseSolvedRuleSets()
		{
			foreach (RuleSet rule in mRules)
			{
				if (rule.IsSolved())
				{
					int i = rule.mIndex;
					int max = rule.mID == RuleID.RI_ROW ? mMaxWidth : mMaxHeight;
					for (int p = 0; p < max; ++p)
					{
						int idx = rule.mID == RuleID.RI_ROW ? GetIndex(p, i) : GetIndex(i, p);
						if (mBoard[idx] == SquareType.ST_UNKNOWN)
							SetBoardValue(idx, SquareType.ST_WHITE);
                    }
				}
			}
		}

		private int MinSpaceRequired(RuleSet rule)
        {
            int total = 0;
            foreach(RuleValue val in rule.mRules)
                total += val.mValue;

            total += rule.mRules.Count - 1;

            return total;
        }

		private void RedeuceValuesMinMax(RuleSet rule, RuleValue value)
		{
			int x, y;
			//if (rule.mID == RuleID.RI_ROW)
			//{
			//	RedeuceValues
			//	x = direction == Direction.RIGHT ? value.minIndex : value.maxIndex;
			//	y = rule.mIndex;
			//}
			//else
			//{
			//	x = rule.mIndex;
			//	y = direction == Direction.DOWN ? value.minIndex : value.maxIndex;
			//}
			//int idx = GetIndex(x, y);
		}

		private void RedeuceValues(RuleSet rule, RuleValue value, Direction dir)
		{
		}

		private void ClampToLargerRun(RuleSet rule)
		{

			//if (rule.mID == RuleID.RI_COLUMN)
			//	return;
			
			Direction dir = rule.mID == RuleID.RI_ROW ? Direction.RIGHT : Direction.DOWN;
			ClampToLargerRun(rule, dir);
			rule.mRules.Reverse();

			dir = rule.mID == RuleID.RI_ROW ? Direction.LEFT : Direction.UP;
			ClampToLargerRun(rule, dir);
			rule.mRules.Reverse();
		}

		private void ClampToLargerRun(RuleSet rule, Direction dir)
		{
			RuleValue value = null;
			int iValue = -1;
			for (int i = 0; i < rule.mRules.Count; ++i)
			{
				if ((rule.mRules[i].IsSolved() && value == null) || (value != null && rule.mRules[i].mValue <= value.mValue))
					continue;

				if (value == null)
				{
					iValue = i;
					value = rule.mRules[i];
				}
				else
				{
					//int x = value.minIndex, y = rule.mIndex;
					int x, y;
					if (rule.mID == RuleID.RI_ROW)
					{
						x = dir == Direction.RIGHT ? value.minIndex : value.maxIndex;
						y = rule.mIndex;
					}
					else
					{
						x = rule.mIndex;
						y = dir == Direction.DOWN ? value.minIndex : value.maxIndex;
					}
					int idx = GetIndex(x, y);
					int start = -1;
					for (int p = 0; p <= value.GetRange(); ++p)
					{
						if (mBoard[idx] == SquareType.ST_BLACK)
						{
							start = p;
							while (mBoard[idx] == SquareType.ST_BLACK)
							{
								++p;
								if (!ShiftIndex(ref idx, dir))
									break;
							}

							if (p - start > value.mValue)
							{
								if (dir == Direction.RIGHT || dir == Direction.DOWN)
									value.SetMaxIndex(value.minIndex + (start - 2));
								else
									value.SetMinIndex(value.maxIndex - (start - 2));

								break;
							}
						}

						if (!ShiftIndex(ref idx, dir))
							break;
					}

					value = null;
				}

				if (value == null && iValue != -1)
				{
					i = iValue;
					iValue = -1;
				}
			}
		}

		private void SolveFullSet(RuleSet rule, int x, int y)
        {
            int idx = GetIndex(x, y);
            Direction dir = rule.mID == RuleID.RI_COLUMN ? Direction.DOWN : Direction.RIGHT;
            foreach(RuleValue v in rule.mRules)
            {
                for (int val = v.mValue; val > 0; --val)
                {
                    SetBoardValue(idx, SquareType.ST_BLACK);
                    if (ShiftIndex(ref idx, dir) == false)
                        return;// We should have placed everythging now
                }

                SetBoardValue(idx, SquareType.ST_WHITE);
                ShiftIndex(ref idx, dir);
            }
        }

        private void OnFormShow(object sender, EventArgs e)
        {
            DrawThing();
        }
    }
}
