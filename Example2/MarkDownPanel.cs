// using MonoGame and basic system stuff
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

// using GeonBit UI elements
using GeonBit.UI.Entities;
using GeonBit.UI.Entities.TextValidators;
using GeonBit.UI.DataTypes;
using GeonBit.UI.Utils.Forms;
using HtmlAgilityPack;
using Microsoft.Xna.Framework.Content;

namespace GeonBit.UI.Example {
    public class MarkDownPanel : Panel {
        //public HTMLParagraph Manager; //the sizing is simple, it doesn't work with an unsized inside of an unsized right now.

        private List<(int, int)> Groupings = new List<(int, int)>();

        public MarkDownPanel(Vector2 size, PanelSkin skin = PanelSkin.Default, Anchor anchor = Anchor.Center, Vector2? offset = null) :
            base(size, skin, anchor, offset)
        {
            
        }

        protected override void UpdateDestinationRects()
        {

            // do extra preparation for text entities
            //CalculateOffsets();
            GroupParagraphs();


            // call base function
            base.UpdateDestinationRects();
        }

        public void GroupParagraphs()
        {
            if (_children.Count == 0)
            {
                return;
            }

            List<Vector2> offsets = new List<Vector2>();
            Paragraph curr = null;
            Paragraph next = null;
            int i = 0;
            int j = 0;
            int end = 0;

            for (int k = 0; k < Groupings.Count; k++) {
                i = Groupings[k].Item1;
                end = Groupings[k].Item2;
                while (curr == null && i < end) {
                    curr = _children[i] as Paragraph;
                    i++;
                }

                if (curr != null) {
                    offsets.Add(Vector2.Zero);
                    curr.ParagraphContinuation = false;
                    curr.StartingOffset = offsets[j];
                    curr.CalcTextActualRectWithWrap();
                    offsets.Add(curr.EndingOffset);
                    j++;
                }

                var last = curr;

                while (curr != null) {
                    while (next == null && i < end) {
                        //skip over things until I put more types in here to flow around. Right now just fancy text.
                        next = _children[i] as Paragraph;
                        i++;
                    }

                    if (next != null) {
                        next.ParagraphContinuation = true;
                        next.StartingOffset = next.ParagraphContinuation ? offsets[j] : Vector2.Zero;
                        next.CalcTextActualRectWithWrap();
                        offsets.Add(next.EndingOffset);
                        var text = next.Text;
                        j++;
                    }

                    curr = next;
                    next = null;

                    if (curr != null) {
                        last = curr;
                    }
                }

                if (last != null && k + 1< Groupings.Count) {
                    //space between paragraphs
                    var text = last.Text;
                    if (text != "\n") {
                        AddChild(new Paragraph("\n"), false, end); //add spacing paragraph
                        end += 1;
                        {
                            var item = Groupings[k];
                            item.Item2 += 1;
                            Groupings[k] = item;
                        }
                        for (int m = k+1; m < Groupings.Count; m++) {
                            var item = Groupings[m];
                            item.Item1 += 1;
                            item.Item2 += 1;
                            Groupings[m] = item;
                        }

                        k--; //altered current group. Recalculate it.
                    }
                }
            }
        }

        
        //parse to dom
        /*var html = @"<!DOCTYPE html>
<html>
<body>
<h1>This is <b>bold</b> heading</h1>
<p>This is <u>underlined</u> paragraph</p>
<h2>This is <i>italic</i> heading</h2>
</body>
</html> ";*/
        public int LoadFromHTML(Game game, string text) {


            Trace.WriteLine(text);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(text);

            var current = htmlDoc.DocumentNode.SelectSingleNode("//body");
            var cookies = new List<(HtmlNode, int, Color, Color, MarkDownPanel)>();
            var currentColor = Color.Black;
            var innerColor = currentColor;

            //Trace.WriteLine("start");
            
            MarkDownPanel target = this;

            HandleHtmlNode(current, currentColor, ref innerColor, target);
            //Trace.Write("\n<" + current.Name + ">\n");
            //Trace.WriteLine("");

            //process children
            for (var i=0; i<current.ChildNodes.Count; i++) {
                var child = current.ChildNodes[i];
                //process child
                
                HandleHtmlNode(child, currentColor, ref innerColor, target);

                if (child.ChildNodes.Count > 0) {
                    //walk in to child children
                    //Trace.WriteLine("inside");
                    cookies.Add((current, i, currentColor, innerColor, target));
                    current = child;
                    i = -1;
                    currentColor = innerColor;
                }
                else if (i + 1 == current.ChildNodes.Count){ 
                    //walk out and finish off each child
                    //Trace.Write("\n</" + child.Name + ">\n"); //completely done with this child
                    HandleCloseHtmlNode(child, currentColor, ref innerColor, target);
                    while (i + 1 == current.ChildNodes.Count && cookies.Count > 0) {
                        //if this is the end see about walking out
                        (current, i, currentColor, innerColor, target) = cookies.Last();
                        cookies.RemoveAt(cookies.Count - 1);
                        //Trace.WriteLine( "outside");
                        //Trace.Write("\n</" + current.ChildNodes[i].Name + ">\n"); //completely done with this current
                        HandleCloseHtmlNode(current.ChildNodes[i], currentColor, ref innerColor, target);
                    }
                }
                else {
                    //Trace.Write("\n</" + child.Name + ">\n"); //completely done with this child
                    HandleCloseHtmlNode(child, currentColor, ref innerColor, target);
                }
            }

            HandleCloseHtmlNode(current, currentColor, ref innerColor, target);
            
            return 0;
        }


        private static void HandleCloseHtmlNode(HtmlNode current, Color currentColor, ref Color innerColor,
            MarkDownPanel target) {
            var currentparagraph = (current.Name == "p") ? current : null;
            if (currentparagraph != null) {
                target.Groupings[target.Groupings.Count - 1] = (target.Groupings.Last().Item1, target.Children.Count); //this and next children need to be in a group
                target.GroupParagraphs();
            }
            //if (current.Name == "p") {
                /*
                bool firstparagraph = true;
                foreach (var child in target.Children) {
                    var textchild = child as Paragraph;
                    if (textchild != null) {
                        textchild.ParagraphContinuation = !firstparagraph;
                        string text = textchild.Text;
                        firstparagraph = false;
                    }
                }*/
            //}
        }

        private static void HandleHtmlNode(HtmlNode current, Color currentColor, ref Color innerColor, MarkDownPanel target) {
            //process current
            //Trace.WriteLine("----");
            Color drawColor = currentColor;
            
            //Trace.Write("<" + current.Name + " ");
            foreach (var attr in current.Attributes) {
                //Trace.Write(attr.Name + " " + attr.Value + " ");
                if (attr.Name == "class") {
                    if (attr.Value.StartsWith("TechForPeace_Color_")) {
                        //add error handling
                        System.Drawing.Color c1 =
                            System.Drawing.Color.FromName(attr.Value.Substring("TechForPeace_Color_".Length));
                        innerColor = new Color(c1.R, c1.G, c1.B, c1.A);
                        drawColor = innerColor;
                    }
                }
            }

            //Trace.Write(">\n");
            var currenttext = current as HtmlTextNode;
            if (currenttext != null) {
                //Trace.Write(currenttext.Text + " ");
                if (target.Groupings.Count > 0 && target.Groupings.Last().Item2 == -1) {
                    //in a html paragraph zone ->
                    var p = target.AddChild(new Paragraph(currenttext.Text));
                    p.FillColor =
                        drawColor; //, Anchor.Auto, currentColor)); //Draw this node with the attribute color as well.
                    p.OutlineWidth = 0;
                }
                //else otherwise ignore text outside of paragraphs for now, whitespace has to be sucked up...
            }

            var currentparagraph = (current.Name == "p") ? current : null;
            if (currentparagraph != null) {
                target.Groupings.Add((target.Children.Count, -1)); //this and next children need to be in a group
            }
            
        }
    }
}