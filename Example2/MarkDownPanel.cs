// using MonoGame and basic system stuff
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

// using GeonBit UI elements
using GeonBit.UI.Entities;
using GeonBit.UI.Entities.TextValidators;
using GeonBit.UI.DataTypes;
using GeonBit.UI.Utils.Forms;
using HtmlAgilityPack;
using Microsoft.Xna.Framework.Content;

namespace GeonBit.UI.Example {
    public class MarkDownPanel : Panel {
        public MarkDownPanel(Vector2 size, PanelSkin skin = PanelSkin.Default, Anchor anchor = Anchor.Center, Vector2? offset = null) :
            base(size, skin, anchor, offset)
        {
            
        }

        public int LoadFromHTML(Game game, string text) {

            //parse to dom
            /*var html = @"<!DOCTYPE html>
<html>
<body>
	<h1>This is <b>bold</b> heading</h1>
	<p>This is <u>underlined</u> paragraph</p>
	<h2>This is <i>italic</i> heading</h2>
</body>
</html> ";*/

            Trace.WriteLine(text);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(text);

            var current = htmlDoc.DocumentNode;
            var cookies = new List<(HtmlNode, int)>();

            Trace.WriteLine("start");

            //process current
            //Trace.WriteLine("----");
            Trace.Write("<" + current.Name + " ");
            foreach (var attr in current.Attributes) {
                Trace.Write(attr.Name + " " + attr.Value + " ");
            }
            Trace.Write(">\n");
            var currenttext = current as HtmlTextNode;
            if (currenttext != null) {
                Trace.Write(currenttext.Text + " ");
            }
            //Trace.Write("\n<" + current.Name + ">\n");
            Trace.WriteLine("");

            for (var i=0; i<current.ChildNodes.Count; i++) {
                var child = current.ChildNodes[i];
                //process child
                //Trace.WriteLine("----");
                Trace.Write("<"+child.Name+" ");
                foreach (var attr in child.Attributes) {
                    Trace.Write(attr.Name + " " + attr.Value + " ");
                }
                Trace.Write(">\n");

                currenttext = child as HtmlTextNode;
                if (currenttext != null) {
                    Trace.Write(currenttext.Text);
                }
                Trace.WriteLine("");

                if (child.ChildNodes.Count > 0) {
                    //walk in to child children
                    Trace.WriteLine("inside");
                    cookies.Add((current, i));
                    current = child;
                    i = -1;
                }
                else if (i + 1 == current.ChildNodes.Count){ //walk out and finish off each child
                    Trace.Write("\n</" + child.Name + ">\n"); //completely done with this child
                    while (i + 1 == current.ChildNodes.Count && cookies.Count > 0) {
                        //if this is the end see about walking out
                        (current, i) = cookies.Last();
                        cookies.RemoveAt(cookies.Count - 1);
                        Trace.WriteLine( "outside");
                        Trace.Write("\n</" + current.ChildNodes[i].Name + ">\n"); //completely done with this current
                    }
                }
                else {
                    Trace.Write("\n</" + child.Name + ">\n"); //completely done with this child
                }
            }
            
            Trace.Write("\n</" + current.Name + ">\n");
            
            Trace.WriteLine("End");

            //var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

            //string htmltxt = htmlBody.OuterHtml; 

            //Console.WriteLine(htmlBody.OuterHtml);
            
            ///build outside to inside, top to bottom
            
            /*
             * Html Agility Pack
HTML Traversing
Traversing allow you to traverse through HTML node.

Properties
Name	Description
ChildNodes	Gets all the children of the node.
FirstChild	Gets the first child of the node.
LastChild	Gets the last child of the node.
NextSibling	Gets the HTML node immediately following this element.
ParentNode	Gets the parent of this node (for nodes that can have parents).
Methods
Name	Description
Ancestors()	Gets all the ancestors of the node.
Ancestors(String)	Gets ancestors with matching names.
AncestorsAndSelf()	Gets all anscestor nodes and the current node.
AncestorsAndSelf(String)	Gets all anscestor nodes and the current node with matching name.
DescendantNodes	Gets all descendant nodes for this node and each of child nodes
DescendantNodesAndSelf	Returns a collection of all descendant nodes of this element, in document order
Descendants()	Gets all descendant nodes in enumerated list
Descendants(String)	Get all descendant nodes with matching names
DescendantsAndSelf()	Returns a collection of all descendant nodes of this element, in document order
DescendantsAndSelf(String)	Gets all descendant nodes including this node
Element	Gets first generation child node matching name
Elements	Gets matching first generation child nodes matching name
             */

            // add title and text
            Image title = new Image(game.Content.Load<Texture2D>("example/GeonBitUI-sm"), new Vector2(400, 240), anchor: Anchor.TopCenter, offset: new Vector2(0, -20));
            title.ShadowColor = new Color(0, 0, 0, 128);
            title.ShadowOffset = Vector2.One * -6;
            this.AddChild(title);
            var welcomeText = new RichParagraph(@"Welcome to {{RED}}GeonBit{{MAGENTA}}.UI{{DEFAULT}}!

GeonBit.UI is the UI system of the GeonBit project.
It provide a simple yet extensive UI for MonoGame based projects.

To start the demo, please click the {{ITALIC}}'Next'{{DEFAULT}} button on the top bar.

");
            this.AddChild(welcomeText);
            this.AddChild(new Paragraph("V" + UserInterface.VERSION, Anchor.BottomRight)).FillColor = Color.Yellow;

            return 0;
        }
    }
}