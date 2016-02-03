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
        //List<List<int>> mRowRules = new List<List<int>>();
        //List<List<int>> mColRules = new List<List<int>>();

        //List<RuleSet> mRowRules = new List<RuleSet>();
        //List<RuleSet> mColRules = new List<RuleSet>();
        List<RuleSet> mRules = new List<RuleSet>();

        public Form1()
        {
            InitializeComponent();
            LoadData();
            Solve();
        }

        private void Solve()
        {
            //SolveFullRules(mRules);
            //SolveFullRules(mColRules);

            CalculateMinMax();
			SolveOverlapRules();
            //SolveOverlapRules(mRowRules);

			BleedEdges();
			//BleedEdges();
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
            foreach(RuleSet rule in mRules)
				CalculateRuleSetMinMax(rule);
        }

		private void CalculateRuleSetMinMax(RuleSet rule)
		{
			int p = 0;
			foreach (RuleValue value in rule.mRules)
			{
				if (value.minIndex == -1 || p > value.minIndex)
					value.minIndex = p;
				else
					p = value.minIndex;
				p += value.mValue + 1;
			}

			p = (rule.mID == RuleID.RI_COLUMN ? mMaxHeight : mMaxWidth) - 1;
			rule.mRules.Reverse();
			foreach (RuleValue value in rule.mRules)
			{
				if (value.maxIndex == -1 || p < value.maxIndex)
					value.maxIndex = p;
				else
					p = value.maxIndex;
				p -= value.mValue + 1;
			}
			rule.mRules.Reverse();
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
				//if (rule.mID == RuleID.RI_COLUMN)
				//if (rule.mID == RuleID.RI_ROW)
				//	continue;

				Direction dir = (rule.mID == RuleID.RI_ROW ? Direction.RIGHT : Direction.DOWN);
				foreach (RuleValue value in rule.mRules)
				{	
					if (BleedValue(rule, value, dir) == false)
						break;
				}
				rule.mRules.Reverse();

				dir = (rule.mID == RuleID.RI_ROW ? Direction.LEFT : Direction.UP);
				foreach (RuleValue value in rule.mRules)
				{
					if (BleedValue(rule, value, dir) == false)
						break;
				}
				rule.mRules.Reverse();
			}
		}

		private bool BleedValue(RuleSet rule, RuleValue value, Direction direction)
		{
			// Make sure these are up-to-date. 
			CalculateRuleSetMinMax(rule);

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

				return true;
			}

			bool foundPortion = false;
			bool isValid = false;
			for (int i = 0; i < value.mValue; ++i)
			{
				if (mBoard[idx] == SquareType.ST_UNKNOWN) // no new information, 
				{
					if (foundPortion)
					{

					}

					isValid = ShiftIndex(ref idx, direction);
					continue;
				}

				if (mBoard[idx] == SquareType.ST_WHITE) // It's not possible to fit in this space. Blank it out, and move ourselves forward
				{
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
					foundPortion = true;
					if (direction == Direction.RIGHT || direction == Direction.DOWN)
					{
						value.maxIndex = Math.Min(value.maxIndex, value.minIndex + i + (value.mValue - 1));
					}
					else
					{
						value.minIndex = Math.Max(value.minIndex, value.maxIndex - i - (value.mValue - 1));
					}
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

			return false;
		}

        private int MinSpaceRequired(RuleSet rule)
        {
            int total = 0;
            foreach(RuleValue val in rule.mRules)
                total += val.mValue;

            total += rule.mRules.Count - 1;

            return total;
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
