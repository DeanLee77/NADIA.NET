﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Nadia.C.Sharp.FactValueFolder;
using Nadia.C.Sharp.RuleParserFolder;

namespace Nadia.C.Sharp.NodeFolder
{
    public class ExprConclusionLine<T> : Node<T>
    {
        private FactValue<T> equation;
        private string dateFormatter = @"dd/MM/yyyy";


        public ExprConclusionLine(string parentText, Tokens tokens): base(parentText, tokens)
        {

        }

        public override void Initialisation(string parentText, Tokens tokens)
        {
            this.nodeName = parentText;
            string[] tempArray = Regex.Split(parentText, "IS CALC");
            variableName = tempArray[0].Trim();
            int indexOfCInTokensStringList = tokens.tokensStringList.IndexOf("C");
            this.SetValue(tokens.tokensStringList[indexOfCInTokensStringList].Trim(), tokens.tokensList[indexOfCInTokensStringList].Trim());
            this.equation = this.value;
        }


        public FactValue<T> GetEquation()
        {
            return this.equation;
        }
        public void SetEquation(FactValue<T> newEquation)
        {
            this.equation = newEquation;
        }

       

        public override LineType GetLineType()
        {
            return LineType.EXPR_CONCLUSION;
        }


        public override FactValue<T> SelfEvaluate(Dictionary<string, FactValue<T>> workingMemory, Jint.Engine jint)
        {
            /*
             * calculation can only handle int, double(long) and difference in years between two dates at the moment.
             * if difference in days or months is required then new 'keyword' must be introduced such as 'Diff Years', 'Diff Days', or 'Diff Months'
             */
            string equationInString = this.equation.GetValue().ToString();
            string pattern = @"[-+/*()?:;,.""](\s*)";
            string datePattern = @"([0-2]?[0-9]|3[0-1])/(0?[0-9]|1[0-2])/([0-9][0-9])?[0-9][0-9]|([0-9][0-9])?[0-9][0-9]/(0?[0-9]|1[0-2])/([0-2]?[0-9]|3[0-1])";


            /*
             * logic for this is as follows;
             *  1. replace all variables with actual values from 'workingMemory'
             *  2. find out if equation is about date (difference in years) calculation or not
             *  3. if it is about date then call 'java.time.LocalDate'and 'java.time.temporal.ChronoUnit' package then do the calculation
             *  3-1. if it is about int or double(long) then use plain Javascript
             *  
             */

            string script = equationInString;
            string tempScript = script;
            string result;

            if (Regex.IsMatch(equationInString, pattern))
            {
                string[] tempArray = Regex.Split(equationInString, pattern);
                int tempArrayLength = tempArray.Length;
                string tempItem;
                for (int i = 0; i < tempArrayLength; i++)
                {
                    tempItem = tempArray[i];
                    if (!string.IsNullOrEmpty(tempItem.Trim()) && workingMemory[tempItem.Trim()] != null)
                    {
                        FactValue<T> tempFv = workingMemory[tempItem.Trim()];
                        if (tempFv.GetValue().GetType().FullName.Equals(DateTime.Now.GetType().FullName))
                        {
                            /*
                             * below line is temporary solution.
                             * Within next iteration it needs to be that this node should take dateFormatter for its constructor to determine which date format it needs
                             */
                            string tempStr = DateTime.ParseExact(tempFv.GetValue().ToString(), dateFormatter, CultureInfo.InvariantCulture).ToString();
                            tempScript = tempScript.Replace(tempScript.Trim(), tempStr);
                        }
                        else
                        {
                            tempScript = tempScript.Replace(tempItem.Trim(), workingMemory[tempItem.Trim()].GetValue().ToString().Trim());
                        }
                    
                    }
                }
            }



            MatchCollection dateMatcher = Regex.Matches(tempScript, datePattern);
            List<DateTime> dateTimeList = new List<DateTime>();
            if(dateMatcher.Count > 0)
            {
                foreach(Match match in dateMatcher)
                {
                    string[] datetimeString = Regex.Split(match.Groups[0].ToString(),@"/");
                    int year = 0;
                    int month = 0;
                    int day = 0;
                    if(Int32.TryParse(datetimeString[2], out year) && Int32.TryParse(datetimeString[1], out month) && Int32.TryParse(datetimeString[0], out day))
                    {
                        dateTimeList.Add(new DateTime(year, month, day));
                    }
                }

                result = (dateTimeList[0].Subtract(dateTimeList[1]).TotalDays / 365.25).ToString();
            }
            else
            {
                result = jint.Execute(tempScript).GetCompletionValue().ToString();
            }
           
            FactValue<T> returnValue = null;

            switch(Tokenizer.GetTokens(result).tokensString)
            {
                case "No":
                    int intResult = 0;
                    Int32.TryParse(result, out intResult);
                    returnValue = FactValue<T>.Parse(intResult);
                    break;
                case "De":
                    returnValue = FactValue<T>.Parse(Convert.ToDouble(result));
                    break;
                //there is no function for outcome to be a date at the moment  E.g.The determination IS CALC(enrollment date + 5 days)
                //case "Da":
                default:
                    returnValue = FactValue<T>.Parse(result);
                    break;
                    
            }

            return returnValue;
        }

    }
}
