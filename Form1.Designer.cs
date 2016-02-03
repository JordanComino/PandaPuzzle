using System;

using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace PandaPuzzle
{
    partial class Form1
    {
        enum SquareType
        {
            ST_UNKNOWN,
            ST_BLACK,
            ST_WHITE
        };

        enum RuleID
        {
            RI_COLUMN,
            RI_ROW
        };

        enum Direction
        {
            UP,
            DOWN,
            LEFT,
            RIGHT
        };

        class RuleValue
        {
            public int mValue;
            public List<int> mIndexs = new List<int>();
            public int minIndex = -1;
            public int maxIndex = -1;

            public RuleValue(int value)
            {
                mValue = value;
            }

            public bool IsSolved()
            {
                return mIndexs.Count == mValue;
            }

			// Asigns us an index that we definately own. Returns true if this is the first time we've tried to add it.
			public bool AddIndex(int idx)
			{
				if (mIndexs.Contains(idx))
					return false;

				mIndexs.Add(idx);
				return true;
			}
        };

        class RuleSet
        {
            public RuleID mID;
            public int mIndex;
            public List<RuleValue> mRules = new List<RuleValue>();
            
            public RuleSet(int index, RuleID id)
            {
                mIndex = index;
                mID = id;
            }

            public bool IsSolved()
            {
                foreach(RuleValue rule in mRules)
                {
                    if (!rule.IsSolved())
                        return false;
                }

                return true;
            }
        };


        private static bool DRAW_GRID = true;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void LoadData()
        {
            Debug.Assert(File.Exists("board.txt"));
            Debug.Assert(File.Exists("rules.txt"));

            // Read in the known state of the board
            string[] board = System.IO.File.ReadAllLines("board.txt");
            if (board.Length > 0)
            {
                mMaxHeight = board.Length;
                mMaxWidth = board[0].Length;

                for (int y = 0; y < mMaxHeight; ++y)
                {
                    for (int x = 0; x < mMaxWidth; ++x)
                    {
                        SquareType type = (SquareType)char.GetNumericValue(board[y][x]);
                        mBoard.Add(type);
                    }
                }
            }

            // read in the rules for each line
            string[] rules = File.ReadAllLines("rules.txt");
            //List<RuleSet> curRules = mRowRules;
            int idx = 0, count = 0;
            RuleID id = RuleID.RI_ROW;
            if (rules.Length > 0)
            {
                foreach(string rule in rules)
                {
                    if (rule == "") // we're now reading the columns
                    {
                        //curRules = mColRules;
                        idx = 0;
                        id = RuleID.RI_COLUMN;
                        continue;
                    }

                    //curRules.Add(new RuleSet(num, id));v
                    mRules.Add(new RuleSet(idx, id));
                    string[] values = rule.Split(',');
                    foreach (string val in values)
                    {
                        //curRules[num].mRules.Add(new RuleValue(Convert.ToInt32(val)));
                        mRules[count].mRules.Add(new RuleValue(Convert.ToInt32(val)));
                    }

                    ++count;
                    ++idx;
                }
            }

            // Varify we have all the information
            // todo: change this so we can just have one set of rules. 
            //Debug.Assert(mRowRules.Count == mMaxHeight);
            //Debug.Assert(mColRules.Count == mMaxWidth);

            // Size the window correctly so we can see everything
            ClientSize = new Size((mSquareSize * mMaxWidth) + (2 * mDrawOffset), (mSquareSize * mMaxHeight) + (2 * mDrawOffset));
        }

        public int GetIndex(int x, int y)
        {
            return (mMaxWidth * y) + x;
        }

		public void GetXY(int idx, out int x, out int y)
		{
			x = idx % mMaxWidth;
			y = (idx - x) / mMaxWidth;
		}

        // Moves your 1D idx around the 2D board, return false if you tried to go off an edge
        private bool ShiftIndex(ref int idx, Direction dir)
        {
            switch(dir)
            {
                case Direction.UP:
                    if (idx >= mMaxWidth)
                    {
                        idx -= mMaxWidth;
                        return true;
                    }
                    break;
                case Direction.DOWN:
                    if(idx < (mMaxWidth * mMaxHeight) - mMaxWidth)
                    {
                        idx += mMaxWidth;
                        return true;
                    }
                    break;
                case Direction.LEFT:
                    if(idx % mMaxWidth > 0)
                    {
                        --idx;
                        return true;
                    }
                    break;
                case Direction.RIGHT:
                    if(idx % mMaxWidth != mMaxWidth-1)
                    {
                        ++idx;
                        return true;
                    }
                    break;
            }

            return false;
        }

        void SetBoardValue(int x, int y, SquareType type)
        {
            int i = GetIndex(x, y);
            SetBoardValue(i, type);
        }

        void SetBoardValue(int idx, SquareType type)
        {
            Debug.Assert(mBoard[idx] == SquareType.ST_UNKNOWN || mBoard[idx] == type); // make sure we haven't set this one yet, or we've calculated it's value to remain the same. Otherwise, there's a logic error somewhere
            mBoard[idx] = type;
        }

        public void DrawThing()
        {
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            SolidBrush whiteBrush = new SolidBrush(Color.White);
            SolidBrush unknownBrush = new SolidBrush(Color.Khaki);
            SolidBrush brush = null;
            Pen linePen = new Pen(Color.Black);
            Graphics formGraphics = CreateGraphics();

            for (int y = 0; y < mMaxHeight; ++y)
            {
                for (int x = 0; x < mMaxWidth; ++x)
                {
                    int idx = GetIndex(x, y);
                    switch (mBoard[idx])
                    {
                        case SquareType.ST_UNKNOWN:
                            brush = unknownBrush;
                            break;
                        case SquareType.ST_WHITE:
                            brush = whiteBrush;
                            break;
                        case SquareType.ST_BLACK:
                            brush = blackBrush;
                            break;
                    }
                    formGraphics.FillRectangle(brush, new Rectangle(mDrawOffset + x * mSquareSize, mDrawOffset + y * mSquareSize, mSquareSize, mSquareSize));
                }
            }

            if (DRAW_GRID)
            {
                for (int x = 0; x <= mMaxWidth; ++x)
                    formGraphics.DrawLine(linePen, x * mSquareSize + mDrawOffset, 0, x * mSquareSize + mDrawOffset, ClientSize.Height);

                for (int y = 0; y <= mMaxHeight; ++y)
                    formGraphics.DrawLine(linePen, 0, y * mSquareSize + mDrawOffset, ClientSize.Width, y * mSquareSize + mDrawOffset);
            }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Shown += new System.EventHandler(this.OnFormShow);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList imageList1;
    }
}

