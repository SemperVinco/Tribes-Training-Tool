﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProcessMemoryReaderLib;
using System.Globalization;
using System.Diagnostics;
using System.IO;



namespace Tribes_Training_Tool {
    public partial class Form1 : Form {
        Process[] myProcess;
        ProcessModule mainModule;
        ProcessMemoryReader Mem = new ProcessMemoryReader();

        #region ------Addresses-------
        int[] PlayerOffsets = new int[]{0x238,  //Health
                                        0x4,    //X Pos
                                        0x8,    //Y Pos
                                        0x0,    //Z Pos
                                        0x90,   //X Speed
                                        0x94,   //Y Speed
                                        0x8C,   //Z Speed     
                                        0x23C   //Max Health
                                        };
        int MainOffset = 0x238;


        int[] BaseTurretOffsets = new int[]{0x0,  //Health
                                            0x4,  //Max Health
                                            0x38, //Armor
                                            0x3C, //Armor Max
                                            0x44, //Recharge rate
                                            0x4C, //Marker Offset
                                            0x5C, //Onhit armor show time     
                                            0x60  //armor show timer
                                            };

        PlayerData m_mainPlayer = new PlayerData(0x00FDF330, new int[] { 0x310, 0x43c, 0x0, 0x1e4, 0x2F4 });
        /*PlayerData[] m_AiPlayer = new PlayerData[] { new PlayerData(0x00FC65A4, new int[] { 0x28, 0xD4, 0x1D8, 0x94, 0x2F4 }),
                                                    new PlayerData(0x00FA7600, new int[] { 0x790, 0x94, 0x760, 0x90, 0x2F4 })};
        */
        PlayerData[] m_BaseTurrets = new PlayerData[] { new PlayerData(0x00FE0F6C, new int[] { 0x60, 0x28, 0x4CC, 0x28, 0x430 }) };

        #endregion

        #region ------Map Spawns-------
        Vector3[,] DryDockSpawns = new Vector3[,] {{new Vector3(-22315,7345,13115),     //Blood Eagle
                                                   new Vector3(-22273,6833,13649),
                                                   new Vector3(-15200,8625,-16672),
                                                   new Vector3(-16641, 5425, -525)},

                                                   {new Vector3(22326,7346,12967),     //Diamond Sword
                                                   new Vector3(22240,6833,13776),
                                                   new Vector3(15745,8625,-16816),
                                                   new Vector3(16608, 5426, -511)}};


        Vector3[,] CrossfireSpawns = new Vector3[,] {{new Vector3(-1967,7437,18017),     //Blood Eagle
                                                   new Vector3(-6703,8251,19215),
                                                   new Vector3(7036,8853,20401),
                                                   new Vector3(-560,7425,17408)},

                                                   {new Vector3(880, 7436, -18000),     //Diamond Sword
                                                   new Vector3(6111, 8252, -19791),
                                                   new Vector3(-7632, 8853, -20978),
                                                   new Vector3(-560,7425,-17408)}};

        Vector3[,] ArxSpawns = new Vector3[,]    {{new Vector3(-23960,6250,-14871),     //Blood Eagle
                                                   new Vector3(-16593,5285,8914),
                                                   new Vector3(-10823, 4859, -15252),
                                                   new Vector3(-15542, 5106, -8432)},

                                                   {new Vector3(23792, 6238, 14720),     //Diamond Sword
                                                   new Vector3(16585, 5286, -8914),
                                                   new Vector3(10541, 4894, 15708),
                                                   new Vector3(15540, 5105, 8445)}};

        Vector3[,] KatabaticSpawns = new Vector3[,]{{new Vector3(-11635, 10665, 20520),     //Blood Eagle
                                                   new Vector3(-16250, 10945, 22710),
                                                   new Vector3(-19020, 11330, 5725),
                                                   new Vector3(-17760, 9330, 13565)},

                                                   {new Vector3(14465, 10670, -16310),     //Diamond Sword
                                                   new Vector3(25245, 11135, -5835),
                                                   new Vector3(15175, 10275, -19340),
                                                   new Vector3(19700, 9330, -11260)}};

        Vector3[,] RaindanceSpawns = new Vector3[,] { { new Vector3(30964, 376,46278),
                                                        new Vector3(44352,-749,51466),
                                                        new Vector3(37134,-140,70014),
                                                        new Vector3(39967,-1707,55045)},

                                                      { new Vector3(100185,338,84566),
                                                        new Vector3(86571,-853,79271),
                                                        new Vector3(93938,-142,61057),
                                                        new Vector3(91112,-1706,76030)} };

        Vector3[,] DXSpawns = new Vector3[,] { { new Vector3(7581,4566,21770),
                                                 new Vector3(4837,6100,32100),
                                                 new Vector3(-7762,2626,17386),
                                                 new Vector3(-2815,2319,17419)}, {

                                                 new Vector3(7742,4583,-21610),
                                                 new Vector3(5000,6100,-32258),
                                                 new Vector3(-7762,2618,-17706),
                                                 new Vector3(-2815,2319,-17395)} };

        Vector3[,] SunstarSpawns = new Vector3[,] { { new Vector3(-16630,9090,-17155),
                                                      new Vector3(-21538,7175,-1119),
                                                      new Vector3(-20466,7212,15865),
                                                      new Vector3(-16858,5743,3064)}, {

                                                      new Vector3(16896,9109,-17235),
                                                      new Vector3(21796,7178,-1022),
                                                      new Vector3(20480,7213,15861),
                                                      new Vector3(16868,5743,3064)} };
        #endregion


        const int PLAYBACKMAXDISTANCE = 750;
        const float INPUTDELAY = 0.5f;

        bool GameFound = false;
        bool m_isRecording = false;
        bool m_isPlayback = false;
        List<Vector3> m_replayPosition = new List<Vector3>();
        List<Vector3> m_replayVelocity = new List<Vector3>();
        int m_playbackState;
        PlayerData m_playBackPlayer;

        float m_inputCooldown;

        public Form1() {
            InitializeComponent();
            KeyPreview=true;
        }

        private void comboBoxGameChosie_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                for(int i = 0; i<myProcess.Length; i++) {
                    //Debug.Print(comboBoxGameChosie.Text.Replace(myProcess[i].ProcessName + "-", ""));
                    if(comboBoxGameChosie.SelectedItem.ToString().Contains(myProcess[i].ProcessName)) {
                        myProcess[0]=Process.GetProcessById(int.Parse(comboBoxGameChosie.Text.Replace(myProcess[i].ProcessName+"-", "")));
                        mainModule=myProcess[0].MainModule;
                        Mem.ReadProcess=myProcess[0];
                        Mem.OpenProcess();
                        GameFound=true;
                        labelGameStatus.Text="Ready";
                    }
                }
            } catch(Exception ex) {
                MessageBox.Show("Could not connect to process "+ex.Message);
            }
        }

        private void comboBoxGameChosie_Click(object sender, EventArgs e) {
            comboBoxGameChosie.Items.Clear();
            myProcess=Process.GetProcesses();
            for(int i = 0; i<myProcess.Length; i++) {
                comboBoxGameChosie.Items.Add(myProcess[i].ProcessName+"-"+myProcess[i].Id);
            }
        }

        Timer timer = new Timer();
        private void Form1_Load(object sender, EventArgs e) {
            timer.Mode=TimerMode.Periodic;
            timer.Period=5;
            timer.Resolution=1;
            timer.SynchronizingObject=this;

            timer.Tick+=timer1_Tick;

            timer.Start();
#if DEBUG
            stopwatch.Start();
            labelTimerSpeed.Visible=true;
#endif
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            timer.Stop();   //Stop it calling the form when the form is closing down
        }

#if DEBUG
        Stopwatch stopwatch = new Stopwatch();
#endif
        private void timer1_Tick(object sender, EventArgs e) {
#if DEBUG
            labelTimerSpeed.Text = stopwatch.ElapsedMilliseconds.ToString();
            stopwatch.Restart();
#endif

            if(GameFound) {
                UpdatePlayerInfo();

                labelHealth.Text=m_mainPlayer.health.ToString();
                labelXPos.Text=m_mainPlayer.position.x.ToString();
                labelYPos.Text=m_mainPlayer.position.y.ToString();
                labelZPos.Text=m_mainPlayer.position.z.ToString();
                labelXSpeed.Text=m_mainPlayer.velocity.x.ToString();
                labelYSpeed.Text=m_mainPlayer.velocity.y.ToString();
                labelZSpeed.Text=m_mainPlayer.velocity.z.ToString();
                float speedTemp = (float)Math.Sqrt(m_mainPlayer.velocity.x*m_mainPlayer.velocity.x+m_mainPlayer.velocity.y*m_mainPlayer.velocity.y+m_mainPlayer.velocity.z*m_mainPlayer.velocity.z);
                labelSpeed.Text=speedTemp.ToString();
                LabelIngameSpeed.Text=(speedTemp*0.072).ToString();

                CheckInput();
                //Misc();
                CheckRecording();
            }


            try {
                if(myProcess!=null) {
                    if(myProcess[0].HasExited)
                        GameFound=false;
                }
            } catch(Exception ex) {
                MessageBox.Show("There was an error "+ex.Message);
            }

        }



        void CheckInput() {
            bool keyDown = false;
            m_inputCooldown-=0.01f;
            if(m_inputCooldown<=0) {
                if(tabControl1.SelectedIndex==0) {
                    if(Mem.Keystate(Keys.F1)) {
                        SpawnButton(0, 0);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F2)) {
                        SpawnButton(0, 1);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F3)) {
                        SpawnButton(0, 2);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F4)) {
                        SpawnButton(0, 3);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F5)) {
                        SpawnButton(1, 0);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F6)) {
                        SpawnButton(1, 1);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F7)) {
                        SpawnButton(1, 2);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F8)) {
                        SpawnButton(1, 3);
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F9)) {
                        WarpTo();
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F10)) {
                        SavePosition();
                        keyDown=true;
                    }
                } else if(tabControl1.SelectedIndex==1) {
                    if(Mem.Keystate(Keys.F1)) {
                        StartRecording();
                        keyDown=true;
                    } else if(Mem.Keystate(Keys.F2)) {
                        StartPlayback();
                        keyDown=true;
                    }
                }

                if(keyDown) {
                    m_inputCooldown=INPUTDELAY;
                }
            }
        }

        /* void Misc()
        {
            if (checkBoxDrunkMode.Checked)
            {
                DrunkMode();
            }
        }

        Random random = new Random();
        void DrunkMode()
        {
            if (random.Next(750) < 10)
            {
                Vector3 vec = new Vector3();
                vec.x = random.Next(1500) - 750;
                vec.y = 0;
                vec.z = random.Next(1500) - 750;
                SetPlayerPosition(m_mainPlayer, m_mainPlayer.position + new Vector3(0,2,0));
                SetPlayerVelocity(m_mainPlayer,m_mainPlayer.velocity + vec);
            }
        } */

        void UpdatePlayerInfo() {
            int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+m_mainPlayer.baseAddress, 4, m_mainPlayer.multiLevel);

            int hp = Mem.ReadInt(playerBase-MainOffset+PlayerOffsets[0]);

            Vector3 pos = new Vector3(Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[1]),
                                      Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[2]),
                                      Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[3]));

            Vector3 velo = new Vector3(Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[4]),
                                       Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[5]),
                                       Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[6]));



            m_mainPlayer.Update(Mem.ReadInt(playerBase-MainOffset+PlayerOffsets[0]), pos, velo);
        }

        void CheckRecording() {
            if(m_isRecording) {
                int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+m_mainPlayer.baseAddress, 4, m_mainPlayer.multiLevel);
                Vector3 vec = new Vector3();
                vec.x=m_mainPlayer.position.x;
                vec.y=m_mainPlayer.position.y;
                vec.z=m_mainPlayer.position.z;
                m_replayPosition.Add(vec);

                vec=new Vector3();
                vec.x=Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[4]);
                vec.y=Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[5]);
                vec.z=Mem.ReadFloat(playerBase-MainOffset+PlayerOffsets[6]);
                m_replayVelocity.Add(vec);

                labelRecordPlayback.Text="Recording";

            } else if(m_isPlayback) {
                //Stop at the end of the replay or restart if loop is checked
                if(m_playbackState>=m_replayPosition.Count) {
                    if(checkBoxReplayLoop.Checked) {
                        m_playbackState=0;
                        int hp = 1000;
                        SetHealth(m_playBackPlayer, hp);
                        SetPlayerPosition(m_playBackPlayer, m_replayPosition[0]);
                    } else {
                        m_isPlayback=false;
                        return;
                    }
                }

                if(checkBoxReplayLoop.Checked) {
                    //Stop having dead bodies flying around
                    if(m_playBackPlayer.isDead) {
                        return;
                    }

                    if(m_playBackPlayer.respawned) {
                        m_playbackState=0;
                        int hp = 800;
                        SetHealth(m_playBackPlayer, hp);
                        SetPlayerPosition(m_playBackPlayer, m_replayPosition[0]);
                    }
                }

                if(!checkBoxOnlyVelocity.Checked) {
                    float distance = Vector3.SqrDistance(m_playBackPlayer.position, m_replayPosition[m_playbackState]);
                    Debug.WriteLine("d:"+distance);
                    if(distance>PLAYBACKMAXDISTANCE)                                 //Make sure the player doesen't get to desynced
                    {
                        SetPlayerPosition(m_playBackPlayer, m_replayPosition[m_playbackState]);
                    }
                }
                SetPlayerVelocity(m_playBackPlayer, m_replayVelocity[m_playbackState]);
                m_playbackState++;

                labelRecordPlayback.Text="Playback";
            } else {
                labelRecordPlayback.Text="Idle";
            }
        }

        void SetPlayerPosition(PlayerData player, Vector3 pos) {
            int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+player.baseAddress, 4, player.multiLevel);

            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[1], pos.x);
            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[2], pos.y);
            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[3], pos.z);
        }

        void SetPlayerVelocity(PlayerData player, Vector3 velocity) {
            int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+player.baseAddress, 4, player.multiLevel);

            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[4], velocity.x);
            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[5], velocity.y);
            Mem.WriteFloat(playerBase-MainOffset+PlayerOffsets[6], velocity.z);
        }

        void SetHealth(PlayerData player, int health) {
            int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+player.baseAddress, 4, player.multiLevel);

            Mem.WriteInt(playerBase-MainOffset+PlayerOffsets[0], health);
            Mem.WriteInt(playerBase-MainOffset+PlayerOffsets[7], health);
        }

        void SetReplayInfo() {
            labelReplaySize.Text=m_replayPosition.Count()/100+" seconds";
        }

        void SpawnButton(int team, int id) {
            int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+m_mainPlayer.baseAddress, 4, m_mainPlayer.multiLevel);

            int maxHp = Mem.ReadInt(playerBase-MainOffset+PlayerOffsets[7]);

            if(checkBoxResetLife.Checked) {
                SetHealth(m_mainPlayer, maxHp);
            }

            SetPlayerVelocity(m_mainPlayer, new Vector3(0, 0, 0));

            if(comboBoxMap.Text=="DryDock") {
                SetPlayerPosition(m_mainPlayer, DryDockSpawns[team, id]);
            } else if(comboBoxMap.Text=="CrossFire") {
                SetPlayerPosition(m_mainPlayer, CrossfireSpawns[team, id]);
            } else if(comboBoxMap.Text=="Arx Novena") {
                SetPlayerPosition(m_mainPlayer, ArxSpawns[team, id]);
            } else if(comboBoxMap.Text=="Katabatic") {
                SetPlayerPosition(m_mainPlayer, KatabaticSpawns[team, id]);
            } else if(comboBoxMap.Text=="RainDance") {
                SetPlayerPosition(m_mainPlayer, RaindanceSpawns[team, id]);
            } else if(comboBoxMap.Text=="Dangerous Crossing") {
                SetPlayerPosition(m_mainPlayer, DXSpawns[team, id]);
            } else if(comboBoxMap.Text=="Sunstar") {
                SetPlayerPosition(m_mainPlayer, SunstarSpawns[team, id]);
            }
        }

        void WarpTo() {
            Vector3 vec = new Vector3();

            if(!float.TryParse(textBoxWarpX.Text, out vec.x))
                textBoxWarpX.Text="";
            if(!float.TryParse(textBoxWarpY.Text, out vec.y))
                textBoxWarpY.Text="";
            if(!float.TryParse(textBoxWarpZ.Text, out vec.z))
                textBoxWarpZ.Text="";

            if(textBoxWarpX.Text=="")
                vec.x=m_mainPlayer.position.x;
            if(textBoxWarpY.Text=="")
                vec.y=m_mainPlayer.position.y;
            if(textBoxWarpZ.Text=="")
                vec.z=m_mainPlayer.position.z;

            SetPlayerPosition(m_mainPlayer, vec);

            if(CheckBoxResetLifeWarp.Checked) {
                int playerBase = Mem.ReadMultiLevelPointer(myProcess[0].MainModule.BaseAddress.ToInt32()+m_mainPlayer.baseAddress, 4, m_mainPlayer.multiLevel);
                int maxHp = Mem.ReadInt(playerBase-MainOffset+PlayerOffsets[7]);
                SetHealth(m_mainPlayer, maxHp);
            }

            if(checkBoxWarpSaveVelocity.Checked) {
                Vector3 vel = new Vector3();

                if(!float.TryParse(textBoxWarpSpeedX.Text, out vel.x))
                    textBoxWarpSpeedX.Text="";
                if(!float.TryParse(textBoxWarpSpeedY.Text, out vel.y))
                    textBoxWarpSpeedY.Text="";
                if(!float.TryParse(textBoxWarpSpeedZ.Text, out vel.z))
                    textBoxWarpSpeedZ.Text="";

                if(textBoxWarpSpeedX.Text=="")
                    vel.x=m_mainPlayer.velocity.x;
                if(textBoxWarpSpeedY.Text=="")
                    vel.y=m_mainPlayer.velocity.y;
                if(textBoxWarpSpeedZ.Text=="")
                    vel.z=m_mainPlayer.velocity.z;

                SetPlayerVelocity(m_mainPlayer, vel);
            }
        }

        void SavePosition() {
            textBoxWarpX.Text=m_mainPlayer.position.x.ToString();
            textBoxWarpY.Text=m_mainPlayer.position.y.ToString();
            textBoxWarpZ.Text=m_mainPlayer.position.z.ToString();

            if(checkBoxWarpSaveVelocity.Checked) {
                textBoxWarpSpeedX.Text=m_mainPlayer.velocity.x.ToString();
                textBoxWarpSpeedY.Text=m_mainPlayer.velocity.y.ToString();
                textBoxWarpSpeedZ.Text=m_mainPlayer.velocity.z.ToString();
            }
        }

        void StartRecording() {
            m_isRecording=!m_isRecording;

            if(m_isRecording) {
                m_isPlayback=false;
                m_replayPosition.Clear();
                m_replayVelocity.Clear();
            } else {
                SetReplayInfo();
            }
        }

        void StartPlayback() {
            m_isPlayback=!m_isPlayback;

            if(m_isPlayback&&m_replayPosition.Count>0) {
                m_playBackPlayer=m_mainPlayer;

                m_isRecording=false;
                SetReplayInfo();
                m_playbackState=0;
                SetPlayerPosition(m_playBackPlayer, m_replayPosition[0]);
            }
        }


        #region ------BUTTONS-----

        #region HEALTH BUTTONS

        private void buttonSetHealth1_Click(object sender, EventArgs e) {
            SetHealth(m_mainPlayer, 1);
        }

        private void buttonSetHealth900_Click(object sender, EventArgs e) {
            SetHealth(m_mainPlayer, 900);
        }

        private void buttonSetHealth1200_Click(object sender, EventArgs e) {
            SetHealth(m_mainPlayer, 1200);
        }

        private void button2SetHealth1000000_Click(object sender, EventArgs e) {
            SetHealth(m_mainPlayer, 1000000);
        }
        #endregion

        #region SPAWN BUTTONS
        private void buttonBESpawn1_Click(object sender, EventArgs e) {
            SpawnButton(0, 0);
        }

        private void buttonBESpawn2_Click(object sender, EventArgs e) {
            SpawnButton(0, 1);
        }

        private void buttonBESpawn3_Click(object sender, EventArgs e) {
            SpawnButton(0, 2);
        }

        private void buttonBESpawn4_Click(object sender, EventArgs e) {
            SpawnButton(0, 3);
        }

        private void buttonDSSpawn1_Click(object sender, EventArgs e) {
            SpawnButton(1, 0);
        }

        private void buttonDSSpawn2_Click(object sender, EventArgs e) {
            SpawnButton(1, 1);
        }

        private void buttonDSSpawn3_Click(object sender, EventArgs e) {
            SpawnButton(1, 2);
        }

        private void buttonDSSpawn4_Click(object sender, EventArgs e) {
            SpawnButton(1, 3);
        }

        #endregion

        #region REPLAY BUTTONS
        private void buttonRecord_Click(object sender, EventArgs e) {
            StartRecording();
        }

        private void buttonPlayback_Click(object sender, EventArgs e) {
            StartPlayback();
        }




        private void buttonImportReplay_Click(object sender, EventArgs e) {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory="c:\\";
            openFileDialog1.Filter="replay files (*.replay)|*.replay|All files (*.*)|*.*";
            openFileDialog1.FilterIndex=2;
            openFileDialog1.RestoreDirectory=true;

            if(openFileDialog1.ShowDialog()==DialogResult.OK) {
                try {
                    if((myStream=openFileDialog1.OpenFile())!=null) {
                        using(myStream) {
                            m_replayPosition.Clear();
                            m_replayVelocity.Clear();

                            StreamReader sr = new StreamReader(myStream);

                            string line;
                            while((line=sr.ReadLine())!=null) {
                                string[] posSplit = line.Split('.');
                                Vector3 vec = new Vector3(float.Parse(posSplit[0]), float.Parse(posSplit[1]), float.Parse(posSplit[2]));
                                Vector3 vel = new Vector3(float.Parse(posSplit[3]), float.Parse(posSplit[4]), float.Parse(posSplit[5]));
                                m_replayPosition.Add(vec);
                                m_replayVelocity.Add(vel);
                            }

                            SetReplayInfo();
                        }
                    }
                } catch(Exception ex) {
                    MessageBox.Show("Error: Could not read file from disk. Original error: "+ex.Message);
                }
            }
        }

        private void buttonExportReplay_Click(object sender, EventArgs e) {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter="replay files (*.replay)|*.replay|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex=1;
            saveFileDialog1.RestoreDirectory=true;

            if(saveFileDialog1.ShowDialog()==DialogResult.OK) {
                if((myStream=saveFileDialog1.OpenFile())!=null) {
                    StreamWriter sw = new StreamWriter(myStream);
                    sw.Flush();
                    int i = 0;
                    foreach(Vector3 vec in m_replayPosition) {
                        Vector3 vel = m_replayVelocity[i];

                        string str = vec.x+"."+vec.y+"."+vec.z+"."+vel.x+"."+vel.y+"."+vel.z;
                        sw.WriteLine(str);

                        i++;
                    }

                    sw.Close();
                    myStream.Close();
                }
            }
        }

        #endregion

        #region WARP BUTTONS
        private void buttonWarp_Click(object sender, EventArgs e) {
            WarpTo();
        }

        private void buttonSavePos_Click(object sender, EventArgs e) {
            SavePosition();
        }
        #endregion

        #endregion
    }
}
