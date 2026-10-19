using GeekMagicMonitor.Core;
using System.Drawing.Drawing2D;

namespace GeekMagicMonitor;

public sealed class Preview : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public ScreenFrame? Frame {get;set;}
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool MirrorHorizontal {get;set;}
    public Preview() { DoubleBuffered=true; Size=new(360,360); BackColor=Color.Black; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        float scale=Math.Min(Width,Height)/240f;
        e.Graphics.ScaleTransform(scale,scale);
        if(MirrorHorizontal)
        {
            e.Graphics.TranslateTransform(240,0);
            e.Graphics.ScaleTransform(-1,1);
        }
        e.Graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        if(Frame is null) return;
        foreach(var cmd in Frames.Commands(Frame,null))
        {
            int I(string key,int fallback=0)=>cmd.TryGetValue(key,out var value)?Convert.ToInt32(value):fallback;
            string S(string key,string fallback="")=>cmd.TryGetValue(key,out var value)?value.ToString()??fallback:fallback;
            using var brush=new SolidBrush(ColorTranslator.FromHtml(S("color","#ffffff")));
            using var pen=new Pen(brush,1);
            switch(S("type"))
            {
                case "clear":e.Graphics.FillRectangle(brush,0,0,240,240);break;
                case "text":
                    using(var background=new SolidBrush(ColorTranslator.FromHtml(S("bg","#091321"))))
                        e.Graphics.FillRectangle(background,I("x"),I("y"),S("text").Length*6*I("size",1),8*I("size",1));
                    using(var font=new Font("Consolas",I("size",1)*9,FontStyle.Bold,GraphicsUnit.Pixel))
                        e.Graphics.DrawString(S("text"),font,brush,I("x"),I("y")-1,StringFormat.GenericTypographic);
                    break;
                case "rect":e.Graphics.FillRectangle(brush,I("x"),I("y"),I("w"),I("h"));break;
                case "roundrect":
                    float radius=Math.Min(I("r"),Math.Min(I("w"),I("h"))/2f);
                    if(radius<=0){e.Graphics.FillRectangle(brush,I("x"),I("y"),I("w"),I("h"));break;}
                    using(var path=new GraphicsPath())
                    {
                        float x=I("x"),y=I("y"),w=I("w"),h=I("h"),d=radius*2;
                        path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);
                        path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();
                        e.Graphics.FillPath(brush,path);
                    }
                    break;
                case "circle":
                    var circle=new Rectangle(I("x")-I("r"),I("y")-I("r"),I("r")*2,I("r")*2);
                    if(cmd.TryGetValue("fill",out var fill) && fill is false)e.Graphics.DrawEllipse(pen,circle);
                    else e.Graphics.FillEllipse(brush,circle);
                    break;
                case "line":e.Graphics.DrawLine(pen,I("x0"),I("y0"),I("x1"),I("y1"));break;
            }
        }
    }
}
