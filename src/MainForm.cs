using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace NLHETheoryAdvisor
{
    sealed class ComboChoice
    {
        public string Text { get; private set; }
        public object Value { get; private set; }

        public ComboChoice(string text, object value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    class MainForm : Form
    {
        private ComboBox _cbHeroPos;
        private ComboBox _cbVillainPos;
        private ComboBox _cbStreet;
        private ComboBox _cbPotType;
        private ComboBox _cbScenario;
        private ComboBox _cbOpponentProfile;
        private ComboBox _cbRangeShape;
        private ComboBox _cbPlayers;
        private CheckBox _chkPosition;
        private CheckBox _chkAggressor;
        private TextBox _tbHeroCards;
        private TextBox _tbFlop;
        private TextBox _tbTurn;
        private TextBox _tbRiver;
        private TextBox _tbPot;
        private TextBox _tbFacingBet;
        private TextBox _tbEffStack;
        private Label _lblPrimary;
        private Label _lblSecondary;
        private Label _lblSummary;
        private Label _lblConfidence;
        private RichTextBox _rtbReasons;
        private RichTextBox _rtbRefs;
        private RichTextBox _rtbTheory;
        private RichTextBox _rtbPreflop;
        private RichTextBox _rtbLog;
        private ListView _lvMetrics;
        private DataGridView _gridPfMatrix;
        private GroupBox _grpPfGrid;
        private ComboBox _cbPfHeroPos;
        private ComboBox _cbPfVillainPos;
        private ComboBox _cbPfScenario;
        private ComboBox _cbPfPlayers;
        private TextBox _tbPfHand;
        private TextBox _tbPfStack;

        public MainForm()
        {
            Text = "NLHE 理論アドバイザー";
            ClientSize = new Size(980, 760);
            MinimumSize = new Size(860, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Meiryo UI", 9f);

            var bottom = new Panel { Height = 170, Dock = DockStyle.Bottom, Parent = this };
            var tabs = new TabControl { Dock = DockStyle.Fill, Parent = this };
            tabs.Padding = new Point(10, 4);

            var p1 = new TabPage("  状況入力  ");
            var p2 = new TabPage("  推奨  ");
            var p3 = new TabPage("  プリフロップ表  ");
            var p4 = new TabPage("  理論ノート  ");
            tabs.TabPages.Add(p1);
            tabs.TabPages.Add(p2);
            tabs.TabPages.Add(p3);
            tabs.TabPages.Add(p4);

            BuildInputPage(p1);
            BuildResultPage(p2);
            BuildPreflopPage(p3);
            BuildTheoryPage(p4);
            BuildBottomPanel(bottom);

            PopulateDefaults();
            Log("起動しました。Janda ベースの理論近似で推奨を返します。");
        }

        private void BuildInputPage(TabPage page)
        {
            var grp1 = MakeGroup(page, "卓・レンジ前提", 10, 170);
            var grp2 = MakeGroup(page, "ハンド・ボード", 190, 145);
            var grp3 = MakeGroup(page, "ベット情報", 345, 145);
            var grp4 = MakeGroup(page, "用語ガイド", 500, 110);

            int x1 = 15;
            int y = 28;
            AddLabeled(grp1, "Hero Position", x1, y + 3, 100);
            _cbHeroPos = MakeCombo(grp1, x1 + 105, y, 95);
            AddLabeled(grp1, "Villain Position", x1 + 235, y + 3, 105);
            _cbVillainPos = MakeCombo(grp1, x1 + 345, y, 95);
            FillPositionCombo(_cbHeroPos);
            FillPositionCombo(_cbVillainPos);

            y += 34;
            AddLabeled(grp1, "Street", x1, y + 3, 100);
            _cbStreet = MakeCombo(grp1, x1 + 105, y, 150);
            AddLabeled(grp1, "Pot Type", x1 + 285, y + 3, 90);
            _cbPotType = MakeCombo(grp1, x1 + 380, y, 170);
            FillStreetCombo(_cbStreet);
            FillPotTypeCombo(_cbPotType);

            y += 34;
            AddLabeled(grp1, "Scenario", x1, y + 3, 100);
            _cbScenario = MakeCombo(grp1, x1 + 105, y, 270);
            AddLabeled(grp1, "Players", x1 + 405, y + 3, 70);
            _cbPlayers = MakeCombo(grp1, x1 + 480, y, 70);
            FillScenarioCombo(_cbScenario);
            FillPlayersCombo(_cbPlayers);

            y += 34;
            AddLabeled(grp1, "Villain Profile", x1, y + 3, 100);
            _cbOpponentProfile = MakeCombo(grp1, x1 + 105, y, 180);
            AddLabeled(grp1, "Range Shape", x1 + 320, y + 3, 90);
            _cbRangeShape = MakeCombo(grp1, x1 + 415, y, 135);
            FillOpponentProfileCombo(_cbOpponentProfile);
            FillRangeShapeCombo(_cbRangeShape);

            _chkPosition = new CheckBox
            {
                Text = "Hero がポジションあり",
                Location = new Point(580, 32),
                AutoSize = true,
                Parent = grp1
            };
            _chkAggressor = new CheckBox
            {
                Text = "Hero がプリフロップアグレッサー",
                Location = new Point(580, 68),
                AutoSize = true,
                Parent = grp1
            };
            var hint = new Label
            {
                Text = "3人以上では Villain Position は“今その判断の基準にしたい相手”を選んでください。通常は最後に強くアクションした相手、または現在ベットしている相手です。",
                Location = new Point(15, 156),
                Width = 820,
                ForeColor = Color.Gray,
                Parent = grp1
            };
            grp1.Resize += delegate(object s, EventArgs e) { hint.Width = grp1.Width - 30; };

            y = 30;
            AddLabeled(grp2, "Hero Cards", x1, y + 3, 95);
            _tbHeroCards = MakeText(grp2, x1 + 120, y, 120);
            _tbHeroCards.Text = "AsKd";
            AddLabeled(grp2, "Flop", x1 + 280, y + 3, 40);
            _tbFlop = MakeText(grp2, x1 + 335, y, 120);
            _tbFlop.Text = "Qs7d2c";
            AddLabeled(grp2, "Turn", x1 + 490, y + 3, 40);
            _tbTurn = MakeText(grp2, x1 + 545, y, 50);
            AddLabeled(grp2, "River", x1 + 620, y + 3, 45);
            _tbRiver = MakeText(grp2, x1 + 675, y, 50);

            var note = new Label
            {
                Text = "入力例: AsKd / Qs7d2c / 4h / Tc  空欄可。board は flop → turn → river の順で入力。",
                Location = new Point(15, 70),
                Width = 800,
                ForeColor = Color.Gray,
                Parent = grp2
            };
            grp2.Resize += delegate(object s, EventArgs e) { note.Width = grp2.Width - 30; };

            y = 30;
            AddLabeled(grp3, "Pot Size (bb)", x1, y + 3, 95);
            _tbPot = MakeText(grp3, x1 + 120, y, 90);
            AddLabeled(grp3, "Facing Bet (bb)", x1 + 245, y + 3, 105);
            _tbFacingBet = MakeText(grp3, x1 + 360, y, 90);
            AddLabeled(grp3, "Effective Stack (bb)", x1 + 490, y + 3, 125);
            _tbEffStack = MakeText(grp3, x1 + 635, y, 90);

            var btnAnalyze = new Button
            {
                Text = "解析",
                Width = 90,
                Location = new Point(15, 82),
                Parent = grp3
            };
            btnAnalyze.Click += delegate(object s, EventArgs e) { RunAnalysis(); };
            var btnClear = new Button
            {
                Text = "クリア",
                Width = 90,
                Location = new Point(115, 82),
                Parent = grp3
            };
            btnClear.Click += delegate(object s, EventArgs e) { ClearInputs(); };
            var btnCopy = new Button
            {
                Text = "結果をコピー",
                Width = 110,
                Location = new Point(215, 82),
                Parent = grp3
            };
            btnCopy.Click += delegate(object s, EventArgs e) { CopyCurrentRecommendation(); };

            var memo = new Label
            {
                Text = "Street = 現在の段階。Scenario = 今まさに直面している状況。Facing Bet = いまコールするのに必要な額。Effective Stack = 自分と主対象 Villain の残りスタックの小さい方。Multiway では残っている相手のうち実際に取り切れる最小スタックを目安にします。",
                Location = new Point(15, 28),
                Width = 800,
                Height = 70,
                Parent = grp4
            };
            grp4.Resize += delegate(object s, EventArgs e) { memo.Width = grp4.Width - 30; };

            _cbHeroPos.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPositionHint(); };
            _cbVillainPos.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPositionHint(); };
            _cbScenario.SelectedIndexChanged += delegate(object s, EventArgs e) { SyncScenarioToStreet(); };
        }

        private void BuildResultPage(TabPage page)
        {
            var grp1 = MakeGroup(page, "推奨アクション", 10, 110);
            var grp2 = MakeGroup(page, "主な根拠", 130, 220);
            var grp3 = MakeGroup(page, "指標", 360, 220);
            var grp4 = MakeGroup(page, "理論参照", 590, 120);

            _lblPrimary = new Label
            {
                Text = "解析待ち",
                Font = new Font("Meiryo UI", 18f, FontStyle.Bold),
                Location = new Point(18, 30),
                Width = 520,
                Height = 36,
                Parent = grp1
            };
            _lblSecondary = new Label
            {
                Text = "",
                Location = new Point(22, 73),
                Width = 640,
                Height = 20,
                ForeColor = Color.DimGray,
                Parent = grp1
            };
            _lblConfidence = new Label
            {
                Text = "確信度: -",
                Location = new Point(690, 33),
                Width = 120,
                Parent = grp1
            };
            _lblSummary = new Label
            {
                Text = "",
                Location = new Point(690, 58),
                Width = 140,
                Height = 40,
                ForeColor = Color.Gray,
                Parent = grp1
            };

            _rtbReasons = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Meiryo UI", 9f),
                Parent = grp2
            };
            _rtbReasons.BorderStyle = BorderStyle.None;
            _rtbReasons.Location = new Point(10, 20);

            _lvMetrics = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill,
                Parent = grp3
            };
            _lvMetrics.Columns.Add("Metric", 180);
            _lvMetrics.Columns.Add("Value", 520);
            grp3.Resize += delegate(object s, EventArgs e)
            {
                if (_lvMetrics.Columns.Count >= 2)
                {
                    _lvMetrics.Columns[0].Width = 180;
                    _lvMetrics.Columns[1].Width = Math.Max(260, grp3.Width - 210);
                }
            };

            _rtbRefs = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Meiryo UI", 9f),
                Parent = grp4
            };
            _rtbRefs.BorderStyle = BorderStyle.None;
        }

        private void BuildPreflopPage(TabPage page)
        {
            var grp1 = new GroupBox
            {
                Text = "照会条件",
                Location = new Point(10, 10),
                Size = new Size(page.Width - 20, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = page
            };
            _grpPfGrid = new GroupBox
            {
                Text = "13x13 グリッド",
                Location = new Point(10, 136),
                Size = new Size(560, Math.Max(260, page.ClientSize.Height - 146)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                Parent = page
            };
            var grp3 = new GroupBox
            {
                Text = "選択ハンド詳細",
                Location = new Point(580, 136),
                Size = new Size(Math.Max(260, page.ClientSize.Width - 590), Math.Max(260, page.ClientSize.Height - 146)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Parent = page
            };

            int x1 = 15;
            int y = 28;
            AddLabeled(grp1, "Hero Pos", x1, y + 3, 75);
            _cbPfHeroPos = MakeCombo(grp1, x1 + 90, y, 80);
            FillPositionCombo(_cbPfHeroPos);
            AddLabeled(grp1, "Villain Pos", x1 + 200, y + 3, 80);
            _cbPfVillainPos = MakeCombo(grp1, x1 + 290, y, 80);
            FillPositionCombo(_cbPfVillainPos);
            AddLabeled(grp1, "Scenario", x1 + 390, y + 3, 70);
            _cbPfScenario = MakeCombo(grp1, x1 + 470, y, 230);
            FillPreflopScenarioCombo(_cbPfScenario);

            y += 35;
            AddLabeled(grp1, "Players", x1, y + 3, 70);
            _cbPfPlayers = MakeCombo(grp1, x1 + 90, y, 80);
            FillPlayersCombo(_cbPfPlayers);
            AddLabeled(grp1, "Hand", x1 + 200, y + 3, 45);
            _tbPfHand = MakeText(grp1, x1 + 250, y, 85);
            AddLabeled(grp1, "Stack", x1 + 360, y + 3, 45);
            _tbPfStack = MakeText(grp1, x1 + 410, y, 70);

            var btnQuery = new Button
            {
                Text = "照会",
                Width = 90,
                Location = new Point(520, y - 1),
                Parent = grp1
            };
            btnQuery.Click += delegate(object s, EventArgs e) { RunPreflopLookup(); };
            var btnLoadCurrent = new Button
            {
                Text = "現在の入力を反映",
                Width = 140,
                Location = new Point(620, y - 1),
                Parent = grp1
            };
            btnLoadCurrent.Click += delegate(object s, EventArgs e) { LoadCurrentIntoPreflop(); };

            var guide = new Label
            {
                Text = "上三角 = suited、下三角 = offsuit、対角 = pair。セルをクリックするとそのハンドの推奨を右に表示します。Hand 欄には AKo / A5s / AsKd のどれでも入力できます。",
                Location = new Point(15, 88),
                Width = grp1.Width - 30,
                ForeColor = Color.Gray,
                Parent = grp1
            };
            grp1.Resize += delegate(object s, EventArgs e)
            {
                guide.Width = grp1.Width - 30;
            };

            var legend = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Text = "緑: Open  青: Call  橙: 3bet/4bet  黄: Mix  灰: Fold",
                Parent = _grpPfGrid
            };

            _gridPfMatrix = new DataGridView
            {
                Dock = DockStyle.Fill,
                Parent = _grpPfGrid,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                ScrollBars = ScrollBars.Vertical,
                RowHeadersWidth = 58,
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 28 }
            };
            _gridPfMatrix.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridPfMatrix.DefaultCellStyle.Font = new Font("Consolas", 7f, FontStyle.Bold);
            _gridPfMatrix.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridPfMatrix.MouseEnter += delegate(object s, EventArgs e) { _gridPfMatrix.Focus(); };
            _gridPfMatrix.CellClick += OnPreflopMatrixCellClick;
            _gridPfMatrix.CellFormatting += OnPreflopMatrixCellFormatting;
            BuildPreflopMatrix();

            _rtbPreflop = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                Parent = grp3
            };
            _rtbPreflop.BorderStyle = BorderStyle.None;

            _cbPfHeroPos.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPreflopMatrix(); };
            _cbPfVillainPos.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPreflopMatrix(); };
            _cbPfScenario.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPreflopMatrix(); };
            _cbPfPlayers.SelectedIndexChanged += delegate(object s, EventArgs e) { RefreshPreflopMatrix(); };
            _tbPfStack.TextChanged += delegate(object s, EventArgs e) { RefreshPreflopMatrix(); };
            _grpPfGrid.Resize += delegate(object s, EventArgs e) { ResizePreflopMatrix(); };

            Action layoutPanels = delegate()
            {
                int top = 136;
                int height = Math.Max(260, page.ClientSize.Height - top - 10);
                int gridWidth = Math.Max(500, Math.Min(560, page.ClientSize.Width - 300));
                _grpPfGrid.SetBounds(10, top, gridWidth, height);
                grp3.SetBounds(_grpPfGrid.Right + 10, top, Math.Max(260, page.ClientSize.Width - _grpPfGrid.Right - 20), height);
            };
            page.Resize += delegate(object s, EventArgs e) { layoutPanels(); };
            layoutPanels();
        }

        private void BuildTheoryPage(TabPage page)
        {
            _rtbTheory = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Meiryo UI", 9f),
                Parent = page
            };
            _rtbTheory.Text = TheoryNotes.Build();
        }

        private void BuildBottomPanel(Panel panel)
        {
            panel.Padding = new Padding(0);
            var ctrl = new Panel { Height = 40, Dock = DockStyle.Top, Parent = panel };
            var logLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Parent = panel
            };
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var btnAnalyze = new Button
            {
                Text = "▶ 解析",
                Width = 100,
                Location = new Point(8, 6),
                Parent = ctrl
            };
            btnAnalyze.Click += delegate(object s, EventArgs e) { RunAnalysis(); };

            var btnCopy = new Button
            {
                Text = "結果をコピー",
                Width = 110,
                Location = new Point(118, 6),
                Parent = ctrl
            };
            btnCopy.Click += delegate(object s, EventArgs e) { CopyCurrentRecommendation(); };

            var btnPf = new Button
            {
                Text = "PF 照会",
                Width = 90,
                Location = new Point(238, 6),
                Parent = ctrl
            };
            btnPf.Click += delegate(object s, EventArgs e) { RunPreflopLookup(); };

            new Label
            {
                Text = "ログ",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Parent = logLayout
            };
            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9f),
                BackColor = Color.White,
                DetectUrls = false,
                Parent = logLayout
            };
            logLayout.SetCellPosition(_rtbLog, new TableLayoutPanelCellPosition(0, 1));
            _rtbLog.BorderStyle = BorderStyle.FixedSingle;
            _rtbLog.Margin = new Padding(0);
        }

        private void PopulateDefaults()
        {
            SelectComboValue(_cbHeroPos, Position.BTN);
            SelectComboValue(_cbVillainPos, Position.BB);
            SelectComboValue(_cbStreet, Street.Flop);
            SelectComboValue(_cbPotType, PotType.SingleRaised);
            SelectComboValue(_cbScenario, ScenarioType.CheckedToHero);
            SelectComboValue(_cbOpponentProfile, OpponentProfile.TheoryBalanced);
            SelectComboValue(_cbRangeShape, RangeShape.Auto);
            SelectComboValue(_cbPlayers, 2);
            _chkAggressor.Checked = false;
            _tbPot.Text = "6.5";
            _tbFacingBet.Text = "0";
            _tbEffStack.Text = "100";
            RefreshPositionHint();

            SelectComboValue(_cbPfHeroPos, Position.BTN);
            SelectComboValue(_cbPfVillainPos, Position.CO);
            SelectComboValue(_cbPfScenario, ScenarioType.FacingOpen);
            SelectComboValue(_cbPfPlayers, 2);
            _tbPfHand.Text = "A5s";
            _tbPfStack.Text = "100";
            RefreshPreflopMatrix();
            RunPreflopLookup();
        }

        private void RunAnalysis()
        {
            try
            {
                ScenarioInput input;
                string error;
                if (!TryBuildScenarioInput(out input, out error))
                {
                    MessageBox.Show(error, "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Log("入力エラー: " + error);
                    return;
                }

                var recommendation = RecommendationEngine.Analyze(input);
                ShowRecommendation(recommendation);
                Log("解析完了: " + recommendation.PrimaryAction);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "解析エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("解析例外: " + ex.Message);
            }
        }

        private void RunPreflopLookup()
        {
            try
            {
                var hero = GetSelectedValue<Position>(_cbPfHeroPos);
                var villain = GetSelectedValue<Position>(_cbPfVillainPos);
                var scenario = GetSelectedValue<ScenarioType>(_cbPfScenario);
                int players = GetSelectedValue<int>(_cbPfPlayers);
                double stack;
                if (!TryParseDouble(_tbPfStack.Text, out stack))
                {
                    stack = 100.0;
                }

                string hand = PreflopCharts.NormalizeHandInput((_tbPfHand.Text ?? string.Empty).Trim());
                if (string.IsNullOrWhiteSpace(hand))
                {
                    _rtbPreflop.Text = "Hand 欄へ AKo / A5s / AsKd のように入力するか、左の 13x13 グリッドからセルをクリックしてください。";
                    return;
                }

                _tbPfHand.Text = hand;
                var result = PreflopCharts.Analyze(hero, villain, scenario, hand, stack, players);
                ShowPreflopDetail(hand, result);
                SelectPreflopCell(hand);
                Log("PF 照会: " + result.Action);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PF 照会エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCurrentIntoPreflop()
        {
            SelectComboValue(_cbPfHeroPos, GetSelectedValue<Position>(_cbHeroPos));
            SelectComboValue(_cbPfVillainPos, GetSelectedValue<Position>(_cbVillainPos));
            SelectComboValue(_cbPfPlayers, GetSelectedValue<int>(_cbPlayers));
            _tbPfHand.Text = PreflopCharts.NormalizeHandInput(_tbHeroCards.Text);
            _tbPfStack.Text = _tbEffStack.Text;

            var scenario = GetSelectedValue<ScenarioType>(_cbScenario);
            if (scenario == ScenarioType.Unopened || scenario == ScenarioType.FacingOpen || scenario == ScenarioType.Facing3Bet)
            {
                SelectComboValue(_cbPfScenario, scenario);
            }
            else
            {
                SelectComboValue(_cbPfScenario, ScenarioType.FacingOpen);
            }

            RefreshPreflopMatrix();
            RunPreflopLookup();
        }

        private void BuildPreflopMatrix()
        {
            if (_gridPfMatrix == null)
            {
                return;
            }

            string[] ranks = new[] { "A", "K", "Q", "J", "T", "9", "8", "7", "6", "5", "4", "3", "2" };
            _gridPfMatrix.Columns.Clear();
            _gridPfMatrix.Rows.Clear();

            foreach (string rank in ranks)
            {
                var col = new DataGridViewTextBoxColumn();
                col.HeaderText = rank;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.Width = 32;
                _gridPfMatrix.Columns.Add(col);
            }

            for (int row = 0; row < ranks.Length; row++)
            {
                _gridPfMatrix.Rows.Add();
                _gridPfMatrix.Rows[row].HeaderCell.Value = ranks[row];
            }

            RefreshPreflopMatrix();
            ResizePreflopMatrix();
        }

        private void RefreshPreflopMatrix()
        {
            if (_gridPfMatrix == null || _gridPfMatrix.Columns.Count == 0)
            {
                return;
            }

            var hero = GetSelectedValue<Position>(_cbPfHeroPos);
            var villain = GetSelectedValue<Position>(_cbPfVillainPos);
            var scenario = GetSelectedValue<ScenarioType>(_cbPfScenario);
            int players = GetSelectedValue<int>(_cbPfPlayers);
            double stack;
            if (!TryParseDouble(_tbPfStack.Text, out stack))
            {
                stack = 100.0;
            }

            for (int row = 0; row < 13; row++)
            {
                for (int col = 0; col < 13; col++)
                {
                    string handCode = GetMatrixHandCode(row, col);
                    var result = PreflopCharts.Analyze(hero, villain, scenario, handCode, stack, players);
                    var cell = _gridPfMatrix.Rows[row].Cells[col];
                    cell.Value = handCode;
                    cell.Tag = result;
                    cell.ToolTipText = BuildPreflopTooltip(handCode, result);
                    cell.Style.BackColor = GetPreflopActionColor(result);
                    cell.Style.ForeColor = Color.Black;
                    cell.Style.SelectionForeColor = Color.Black;
                    cell.Style.SelectionBackColor = ControlPaint.Dark(GetPreflopActionColor(result));
                }
            }

            string normalizedHand = PreflopCharts.NormalizeHandInput(_tbPfHand.Text);
            if (!string.IsNullOrWhiteSpace(normalizedHand))
            {
                SelectPreflopCell(normalizedHand);
                var selected = GetPreflopResultForHand(normalizedHand);
                if (selected != null)
                {
                    ShowPreflopDetail(normalizedHand, selected);
                }
            }
            ResizePreflopMatrix();
        }

        private void ResizePreflopMatrix()
        {
            if (_gridPfMatrix == null || _gridPfMatrix.Columns.Count == 0)
            {
                return;
            }

            _gridPfMatrix.RowHeadersWidth = 32;
            _gridPfMatrix.ColumnHeadersHeight = 20;

            int clientWidth = _gridPfMatrix.ClientSize.Width - _gridPfMatrix.RowHeadersWidth - 2;
            int colWidth = Math.Max(24, clientWidth / 13);
            for (int i = 0; i < _gridPfMatrix.Columns.Count; i++)
            {
                _gridPfMatrix.Columns[i].Width = colWidth;
            }

            for (int i = 0; i < _gridPfMatrix.Rows.Count; i++)
            {
                _gridPfMatrix.Rows[i].Height = 28;
            }
        }

        private void ShowPreflopDetail(string handCode, PreflopLookupResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hand  : " + handCode);
            sb.AppendLine("Action: " + result.Action);
            if (!string.IsNullOrWhiteSpace(result.SecondaryAction))
            {
                sb.AppendLine("Alt   : " + result.SecondaryAction);
            }
            sb.AppendLine("Spot  : " + result.SpotLabel);
            if (!string.IsNullOrWhiteSpace(result.RangeSummary))
            {
                sb.AppendLine();
                sb.AppendLine(result.RangeSummary);
            }
            if (result.Notes.Count > 0)
            {
                sb.AppendLine();
                foreach (var note in result.Notes)
                {
                    sb.AppendLine("- " + note);
                }
            }
            _rtbPreflop.Text = sb.ToString();
        }

        private void SelectPreflopCell(string handCode)
        {
            if (_gridPfMatrix == null || string.IsNullOrWhiteSpace(handCode))
            {
                return;
            }

            string normalized = PreflopCharts.NormalizeHandInput(handCode);
            for (int row = 0; row < 13; row++)
            {
                for (int col = 0; col < 13; col++)
                {
                    if (GetMatrixHandCode(row, col) == normalized)
                    {
                        _gridPfMatrix.ClearSelection();
                        _gridPfMatrix.CurrentCell = _gridPfMatrix.Rows[row].Cells[col];
                        _gridPfMatrix.Rows[row].Cells[col].Selected = true;
                        return;
                    }
                }
            }
        }

        private PreflopLookupResult GetPreflopResultForHand(string handCode)
        {
            if (_gridPfMatrix == null || string.IsNullOrWhiteSpace(handCode))
            {
                return null;
            }

            string normalized = PreflopCharts.NormalizeHandInput(handCode);
            for (int row = 0; row < 13; row++)
            {
                for (int col = 0; col < 13; col++)
                {
                    if (GetMatrixHandCode(row, col) == normalized)
                    {
                        return _gridPfMatrix.Rows[row].Cells[col].Tag as PreflopLookupResult;
                    }
                }
            }
            return null;
        }

        private void OnPreflopMatrixCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            string handCode = GetMatrixHandCode(e.RowIndex, e.ColumnIndex);
            _tbPfHand.Text = handCode;
            var result = _gridPfMatrix.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag as PreflopLookupResult;
            if (result == null)
            {
                RunPreflopLookup();
                return;
            }

            ShowPreflopDetail(handCode, result);
            Log("PF セル選択: " + handCode + " -> " + result.Action);
        }

        private void OnPreflopMatrixCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private static string GetMatrixHandCode(int rowIndex, int columnIndex)
        {
            char[] ranks = new[] { 'A', 'K', 'Q', 'J', 'T', '9', '8', '7', '6', '5', '4', '3', '2' };
            char rowRank = ranks[rowIndex];
            char colRank = ranks[columnIndex];

            if (rowIndex == columnIndex)
            {
                return new string(new[] { rowRank, rowRank });
            }

            if (rowIndex < columnIndex)
            {
                return string.Concat(rowRank, colRank, 's');
            }

            return string.Concat(colRank, rowRank, 'o');
        }

        private static string BuildPreflopTooltip(string handCode, PreflopLookupResult result)
        {
            if (result == null)
            {
                return handCode;
            }

            var sb = new StringBuilder();
            sb.Append(handCode);
            sb.Append(" : ");
            sb.Append(result.Action);
            if (!string.IsNullOrWhiteSpace(result.SecondaryAction))
            {
                sb.Append(" / ");
                sb.Append(result.SecondaryAction);
            }
            return sb.ToString();
        }

        private static Color GetPreflopActionColor(PreflopLookupResult result)
        {
            if (result == null)
            {
                return Color.White;
            }

            if (!string.IsNullOrWhiteSpace(result.SecondaryAction) || result.Action.IndexOf("混合", StringComparison.Ordinal) >= 0)
            {
                return Color.FromArgb(255, 244, 188);
            }
            if (result.Action.IndexOf("オープン", StringComparison.Ordinal) >= 0)
            {
                return Color.FromArgb(210, 243, 214);
            }
            if (result.Action.IndexOf("コール", StringComparison.Ordinal) >= 0)
            {
                return Color.FromArgb(216, 235, 255);
            }
            if (result.Action.IndexOf("3ベット", StringComparison.Ordinal) >= 0
                || result.Action.IndexOf("4ベット", StringComparison.Ordinal) >= 0
                || result.Action.IndexOf("コミット", StringComparison.Ordinal) >= 0)
            {
                return Color.FromArgb(255, 221, 189);
            }
            if (result.Action.IndexOf("フォールド", StringComparison.Ordinal) >= 0)
            {
                return Color.FromArgb(235, 235, 235);
            }
            return Color.White;
        }

        private void ShowRecommendation(Recommendation rec)
        {
            _lblPrimary.Text = rec.PrimaryAction;
            _lblSecondary.Text = rec.SecondaryAction;
            _lblSummary.Text = rec.Summary;
            _lblConfidence.Text = "確信度: " + rec.Confidence;
            _rtbReasons.Text = BuildLineBlock(rec.Reasons);
            _rtbRefs.Text = BuildLineBlock(rec.TheoryReferences);

            _lvMetrics.Items.Clear();
            foreach (var metric in rec.Metrics)
            {
                var item = new ListViewItem(metric.Key);
                item.SubItems.Add(metric.Value);
                _lvMetrics.Items.Add(item);
            }
        }

        private bool TryBuildScenarioInput(out ScenarioInput input, out string error)
        {
            input = new ScenarioInput();
            error = string.Empty;

            input.HeroPosition = GetSelectedValue<Position>(_cbHeroPos);
            input.VillainPosition = GetSelectedValue<Position>(_cbVillainPos);
            input.Scenario = GetSelectedValue<ScenarioType>(_cbScenario);
            input.Street = IsPreflopScenario(input.Scenario) ? Street.Preflop : GetSelectedValue<Street>(_cbStreet);
            input.PotType = GetSelectedValue<PotType>(_cbPotType);
            input.OpponentProfile = GetSelectedValue<OpponentProfile>(_cbOpponentProfile);
            input.RangeShape = GetSelectedValue<RangeShape>(_cbRangeShape);
            input.Players = GetSelectedValue<int>(_cbPlayers);
            input.HeroHasPosition = _chkPosition.Checked;
            input.HeroWasPreflopAggressor = _chkAggressor.Checked;

            double potSize;
            if (!TryParseDouble(_tbPot.Text, out potSize))
            {
                error = "Pot Size を数値で入力してください。";
                return false;
            }
            input.PotSize = potSize;

            double facingBetSize;
            if (!TryParseDouble(_tbFacingBet.Text, out facingBetSize))
            {
                error = "Facing Bet を数値で入力してください。";
                return false;
            }
            input.FacingBetSize = facingBetSize;

            double effectiveStack;
            if (!TryParseDouble(_tbEffStack.Text, out effectiveStack))
            {
                error = "Effective Stack を数値で入力してください。";
                return false;
            }
            input.EffectiveStack = effectiveStack;

            string cardError;
            List<Card> heroCards;
            if (!Card.TryParseCards(_tbHeroCards.Text, 2, out heroCards, out cardError))
            {
                error = "Hero Cards: " + cardError;
                return false;
            }
            input.HeroCards = heroCards;

            if (input.Street != Street.Preflop)
            {
                List<Card> flopCards;
                if (!Card.TryParseCards(_tbFlop.Text, 3, out flopCards, out cardError))
                {
                    error = "Flop: " + cardError;
                    return false;
                }
                input.FlopCards = flopCards;

                if (input.Street == Street.Turn || input.Street == Street.River)
                {
                    List<Card> turnCards;
                    if (!Card.TryParseCards(_tbTurn.Text, 1, out turnCards, out cardError))
                    {
                        error = "Turn: " + cardError;
                        return false;
                    }
                    input.TurnCard = turnCards[0];
                }

                if (input.Street == Street.River)
                {
                    List<Card> riverCards;
                    if (!Card.TryParseCards(_tbRiver.Text, 1, out riverCards, out cardError))
                    {
                        error = "River: " + cardError;
                        return false;
                    }
                    input.RiverCard = riverCards[0];
                }

                if (HasDuplicateCards(input))
                {
                    error = "Hero / board で同じカードが重複しています。";
                    return false;
                }
            }

            return true;
        }

        private bool HasDuplicateCards(ScenarioInput input)
        {
            var seen = new HashSet<Card>();
            foreach (var card in input.HeroCards)
            {
                if (seen.Contains(card)) return true;
                seen.Add(card);
            }
            foreach (var card in input.GetBoard())
            {
                if (seen.Contains(card)) return true;
                seen.Add(card);
            }
            return false;
        }

        private void SyncScenarioToStreet()
        {
            var scenario = GetSelectedValue<ScenarioType>(_cbScenario);
            if (IsPreflopScenario(scenario))
            {
                SelectComboValue(_cbStreet, Street.Preflop);
            }
        }

        private void RefreshPositionHint()
        {
            try
            {
                var hero = GetSelectedValue<Position>(_cbHeroPos);
                var villain = GetSelectedValue<Position>(_cbVillainPos);
                _chkPosition.Checked = GuessPosition(hero, villain);
            }
            catch
            {
            }
        }

        private static bool GuessPosition(Position hero, Position villain)
        {
            if (hero == Position.BTN) return true;
            if (villain == Position.BTN) return false;
            if (hero == Position.BB && villain == Position.SB) return true;
            if (hero == Position.SB && villain == Position.BB) return false;
            if (hero == Position.SB || hero == Position.BB) return false;
            if (villain == Position.SB || villain == Position.BB) return true;
            return PositionHelper.GetOrder(hero) > PositionHelper.GetOrder(villain);
        }

        private void CopyCurrentRecommendation()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine(_lblPrimary.Text);
                if (!string.IsNullOrWhiteSpace(_lblSecondary.Text))
                {
                    sb.AppendLine(_lblSecondary.Text);
                }
                if (!string.IsNullOrWhiteSpace(_lblSummary.Text))
                {
                    sb.AppendLine(_lblSummary.Text);
                }
                if (!string.IsNullOrWhiteSpace(_rtbReasons.Text))
                {
                    sb.AppendLine();
                    sb.AppendLine(_rtbReasons.Text);
                }
                Clipboard.SetText(sb.ToString());
                Log("結果をクリップボードへコピーしました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "コピー失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearInputs()
        {
            _tbHeroCards.Text = "";
            _tbFlop.Text = "";
            _tbTurn.Text = "";
            _tbRiver.Text = "";
            _tbPot.Text = "0";
            _tbFacingBet.Text = "0";
            _tbEffStack.Text = "100";
            _lblPrimary.Text = "解析待ち";
            _lblSecondary.Text = "";
            _lblSummary.Text = "";
            _rtbReasons.Clear();
            _rtbRefs.Clear();
            _lvMetrics.Items.Clear();
            Log("入力をクリアしました。");
        }

        private void Log(string message)
        {
            if (_rtbLog == null) return;
            _rtbLog.AppendText(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  " + message + Environment.NewLine);
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.ScrollToCaret();
        }

        private static GroupBox MakeGroup(TabPage page, string text, int y, int height)
        {
            var grp = new GroupBox
            {
                Text = text,
                Location = new Point(10, y),
                Height = height,
                Width = page.Width - 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Parent = page
            };
            page.Resize += delegate(object s, EventArgs e)
            {
                grp.Width = page.Width - 20;
            };
            return grp;
        }

        private static ComboBox MakeCombo(Control parent, int x, int y, int width)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Parent = parent
            };
            return cb;
        }

        private static TextBox MakeText(Control parent, int x, int y, int width)
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Parent = parent
            };
            return tb;
        }

        private static void AddLabeled(Control parent, string text, int x, int y)
        {
            new Label
            {
                Text = text,
                Location = new Point(x, y),
                Width = 120,
                Parent = parent
            };
        }

        private static void AddLabeled(Control parent, string text, int x, int y, int width)
        {
            new Label
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                Parent = parent
            };
        }

        private static void FillPositionCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("UTG", Position.UTG));
            cb.Items.Add(new ComboChoice("MP", Position.MP));
            cb.Items.Add(new ComboChoice("CO", Position.CO));
            cb.Items.Add(new ComboChoice("BTN", Position.BTN));
            cb.Items.Add(new ComboChoice("SB", Position.SB));
            cb.Items.Add(new ComboChoice("BB", Position.BB));
            cb.SelectedIndex = 0;
        }

        private static void FillStreetCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Preflop (プリフロップ)", Street.Preflop));
            cb.Items.Add(new ComboChoice("Flop (フロップ)", Street.Flop));
            cb.Items.Add(new ComboChoice("Turn (ターン)", Street.Turn));
            cb.Items.Add(new ComboChoice("River (リバー)", Street.River));
            cb.SelectedIndex = 1;
        }

        private static void FillPotTypeCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Single Raised", PotType.SingleRaised));
            cb.Items.Add(new ComboChoice("3-Bet Pot", PotType.ThreeBet));
            cb.Items.Add(new ComboChoice("4-Bet Pot", PotType.FourBet));
            cb.Items.Add(new ComboChoice("Multiway / 3人以上", PotType.Multiway));
            cb.SelectedIndex = 0;
        }

        private static void FillScenarioCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Unopened: まだ誰も入れていない", ScenarioType.Unopened));
            cb.Items.Add(new ComboChoice("Facing Open: オープンに直面", ScenarioType.FacingOpen));
            cb.Items.Add(new ComboChoice("Facing 3-Bet: 3ベットに直面", ScenarioType.Facing3Bet));
            cb.Items.Add(new ComboChoice("Checked To Hero: 自分にチェックで回った", ScenarioType.CheckedToHero));
            cb.Items.Add(new ComboChoice("Facing Bet: 相手からベットされた", ScenarioType.FacingBet));
            cb.Items.Add(new ComboChoice("Facing Raise: 自分のベット後にレイズされた", ScenarioType.FacingRaise));
            cb.SelectedIndex = 3;
        }

        private static void FillPreflopScenarioCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Unopened: まだ誰も入れていない", ScenarioType.Unopened));
            cb.Items.Add(new ComboChoice("Facing Open: オープンに直面", ScenarioType.FacingOpen));
            cb.Items.Add(new ComboChoice("Facing 3-Bet: 3ベットに直面", ScenarioType.Facing3Bet));
            cb.SelectedIndex = 1;
        }

        private static void FillOpponentProfileCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Theory Balanced", OpponentProfile.TheoryBalanced));
            cb.Items.Add(new ComboChoice("Tight Passive", OpponentProfile.TightPassive));
            cb.Items.Add(new ComboChoice("Loose Aggressive", OpponentProfile.LooseAggressive));
            cb.SelectedIndex = 0;
        }

        private static void FillRangeShapeCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("Auto", RangeShape.Auto));
            cb.Items.Add(new ComboChoice("Polarized", RangeShape.Polarized));
            cb.Items.Add(new ComboChoice("Condensed", RangeShape.Condensed));
            cb.Items.Add(new ComboChoice("Balanced", RangeShape.Balanced));
            cb.SelectedIndex = 0;
        }

        private static void FillPlayersCombo(ComboBox cb)
        {
            cb.Items.Clear();
            cb.Items.Add(new ComboChoice("2", 2));
            cb.Items.Add(new ComboChoice("3", 3));
            cb.Items.Add(new ComboChoice("4", 4));
            cb.Items.Add(new ComboChoice("5", 5));
            cb.Items.Add(new ComboChoice("6", 6));
            cb.SelectedIndex = 0;
        }

        private static T GetSelectedValue<T>(ComboBox cb)
        {
            var choice = cb.SelectedItem as ComboChoice;
            if (choice == null)
            {
                return default(T);
            }
            return (T)choice.Value;
        }

        private static void SelectComboValue(ComboBox cb, object value)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                var choice = cb.Items[i] as ComboChoice;
                if (choice != null && Equals(choice.Value, value))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool IsPreflopScenario(ScenarioType scenario)
        {
            return scenario == ScenarioType.Unopened
                || scenario == ScenarioType.FacingOpen
                || scenario == ScenarioType.Facing3Bet;
        }

        private static string BuildLineBlock(List<string> lines)
        {
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                sb.Append("• ");
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
