using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PluginContracts;
using OutputHelperLib;
using GroupDataObj;


namespace LSM
{
    public partial class LSM : Plugin
    {


        public string[] InputType { get; } = { "GroupData" };
        public string OutputType { get; } = "OutputArray";

        public Dictionary<int, string> OutputHeaderData { get; set; } = new Dictionary<int, string>() { { 0, "P1" },
                                                                                                        { 1, "P2" },
                                                                                                        { 2, "P1_WC" },
                                                                                                        { 3, "P2_WC" },
                                                                                                        { 4, "LSM" },
                                                                                                       };
        public bool InheritHeader { get; } = false;

        #region Plugin Details and Info

        public string PluginName { get; } = "Language Style Matching";
        public string PluginType { get; } = "Dyads & Groups";
        public string PluginVersion { get; } = "1.0.1";
        public string PluginAuthor { get; } = "Ryan L. Boyd (ryan@ryanboyd.io)";
        public string PluginDescription { get; } = "Calculates all pairwise Language Style Matching (LSM) scores for a group of texts. LSM can be thought of as the degree to which two or more people are coordinating their attention, as measured through their verbal behavior. More information on LSM can be found in publications including (but not limited to):" + Environment.NewLine + Environment.NewLine +
            "Ireland, M. E., Slatcher, R. B., Eastwick, P. W., Scissors, L. E., Finkel, E. J., & Pennebaker, J. W. (2010). Language Style Matching Predicts Relationship Initiation and Stability. Psychological Science, 22(1), 39–44. https://doi.org/10.1177/0956797610392928" + Environment.NewLine + Environment.NewLine +
             "Taylor, P. J., Larner, S., Conchie, S. M., & Menacere, T. (2017). Culture moderates changes in linguistic self-presentation and detail provision when deceiving others. Royal Society Open Science, 4(6), 170128. https://doi.org/10.1098/rsos.170128" +Environment.NewLine + Environment.NewLine +
             "Babcock, M. J., Ta, V. P., & Ickes, W. (2013). Latent Semantic Similarity and Language Style Matching in Initial Dyadic Interactions. Journal of Language and Social Psychology, 33(1), 78–88. https://doi.org/10.1177/0261927X13499331" ;
        public string PluginTutorial { get; } = "https://youtu.be/IlA1diE0b9I";
        public bool TopLevel { get; } = false;

        #endregion

        DictionaryMetaObject LSMDict { get; set; }
        TwitterAwareTokenizer tokenizer { get; set; }
        private static string[] stopList { get; } = new string[] { "`", "~", "!", "@", "#", "$", "%", "^", "&", "*", "(",
                                                                    ")", "_", "+", "-", "–", "=", "[", "]", "\\", ";",
                                                                    "'", ",", ".", "/", "{", "}", "|", ":", "\"", "<",
                                                                    ">", "?", "..", "...", "«", "««", "»»", "“", "”",
                                                                    "‘", "‘‘", "’", "’’", "1", "2", "3", "4", "5", "6",
                                                                    "7", "8", "9", "0", "10", "11", "12", "13", "14",
                                                                    "15", "16", "17", "18", "19", "20", "25", "30", "33",
                                                                    "40", "50", "60", "66", "70", "75", "80", "90", "99",
                                                                    "100", "123", "1000", "10000", "12345", "100000", "1000000" };

        public Icon GetPluginIcon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }





        public void ChangeSettings()
        {

            MessageBox.Show("This plugin does not have any settings to change.",
                    "No Settings", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

        }





        public Payload RunPlugin(Payload Input)
        {


            Payload pData = new Payload();
            pData.FileID = Input.FileID;
            //pData.SegmentID = Input.SegmentID;


            for (int i = 0; i < Input.ObjectList.Count; i++)
            {

                //unpack the group
                GroupData group = (GroupData)(Input.ObjectList[i]);

                //set up the tokens and analyze each person's text
                string[][] tokens = new string[group.People.Count][];
                Dictionary<string, int>[] lsmCatResults = new Dictionary<string, int>[group.People.Count];

                for(int j = 0; j < group.People.Count; j++)
                {

                    //rebuild the person's text into a single unit for analysis
                    StringBuilder personText = new StringBuilder();
                    for (int personTurn = 0; personTurn < group.People[j].text.Count; personTurn++) personText.AppendLine(group.People[j].text[personTurn]);

                    tokens[j] = tokenizer.tokenize(personText.ToString()).Where(x => !stopList.Contains(x)).ToArray();
                    lsmCatResults[j] = AnalyzeText(LSMDict.DictData, tokens[j]);

                }


                // go in and actually calculate the LSM scores
                for (int j = 0; j < group.People.Count - 1; j++)
                {

                    for (int k = 1; k + j < group.People.Count; k++)
                    {



                        int TCpOne = tokens[j].Length;
                        int TCpTwo = tokens[j + k].Length;

                        string TextOneID = group.People[j].id;
                        string TextTwoID = group.People[j + k].id;

                        decimal lsmScore = 0.0m;
                        string lsmScoreString = "";

                        if (tokens[j].Length > 0 && tokens[j + k].Length > 0)
                        {

                            //loop from 1 to 8 because those are the category numbers for each LSM category
                            for (int l = 1; l < 9; l++)
                            {

                                decimal pOneScore = (decimal)lsmCatResults[j][l.ToString()] / tokens[j].Length;
                                decimal pTwoScore = (decimal)lsmCatResults[j + k][l.ToString()] / tokens[j + k].Length;

                                lsmScore += 1.0m - (Math.Abs(pOneScore - pTwoScore) / (pOneScore + pTwoScore + 0.0001m));

                            }

                            lsmScoreString = (lsmScore / 8.0m).ToString();

                        }



                        pData.StringArrayList.Add(new string[] { TextOneID,
                                                             TextTwoID,
                                                             tokens[j].Length.ToString(),
                                                             tokens[j+k].Length.ToString(),
                                                             lsmScoreString
                                                            });

                        pData.SegmentNumber.Add(Input.SegmentNumber[i]);
                        pData.SegmentID.Add(TextOneID + ";" + TextTwoID);

                    }

                }





            }
            


            return (pData);

        }




        public void Initialize()
        {

            LSMDict = new DictionaryMetaObject("LSM Dictionary", "Description", "", Properties.Resources.LSM);
            LSMDict.DictData = ParseDict(LSMDict);
            tokenizer = new TwitterAwareTokenizer();

        }

        public bool InspectSettings()
        {
            return true;
        }

        public Payload FinishUp(Payload Input)
        {
            return (Input);
        }


        #region Import/Export Settings
        public void ImportSettings(Dictionary<string, string> SettingsDict)
        {

        }

        public Dictionary<string, string> ExportSettings(bool suppressWarnings)
        {
            Dictionary<string, string> SettingsDict = new Dictionary<string, string>();
            return (SettingsDict);
        }
        #endregion


    }
}
