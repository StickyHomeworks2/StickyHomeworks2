using System.Drawing;
using Color = System.Drawing.Color;

namespace StickyHomeworks;

public class ColorOctTreeNode
{
    public int LeafNum = 0;
    public List<ColorOctTreeNode>[] ToReduce = {
        new List<ColorOctTreeNode>(), new List<ColorOctTreeNode>(),
        new List<ColorOctTreeNode>(), new List<ColorOctTreeNode>(),
        new List<ColorOctTreeNode>(), new List<ColorOctTreeNode>(),
        new List<ColorOctTreeNode>(), new List<ColorOctTreeNode>()
    };

    public ColorOctTreeNode?[] Children = new ColorOctTreeNode?[8] {null, null , null , null , null , null , null , null };
    public bool IsLeaf = false;
    public int r = 0;
    public int g = 0;
    public int b = 0;
    public int ChildrenCount = 0;
    public ColorOctTreeNode Root;

    public ColorOctTreeNode(ColorOctTreeNode? root, int index, int level)
    {
        Root = root ?? this;
        if (root == null)
        {
            return;
        }
        if (level == 7)
        {
            IsLeaf = true;
            Root.LeafNum++;
        }
        else
        {
            Root.ToReduce[level].Add(this);
            Root.ToReduce[level] = Root.ToReduce[level].OrderBy(i => i.ChildrenCount).ToList();
        }
    }

    public void AddColor(Color color, int level)
    {
        if (IsLeaf)
        {
            ChildrenCount++;
            this.r += color.R;
            this.g += color.G;
            this.b += color.B;
        }
        else
        {
            var rByte = color.R;
            var gByte = color.G;
            var bByte = color.B;

            int index = ((rByte >> (7 - level)) & 1) << 2 |
                        ((gByte >> (7 - level)) & 1) << 1 |
                        ((bByte >> (7 - level)) & 1);
            Children[index] ??= new ColorOctTreeNode(Root, index, level + 1);
            Children[index]!.AddColor(color, level + 1);
        }
    }

    public void ReduceTree()
    {
        var lv = 6;
        while (lv >= 0 && Root.ToReduce[lv].Count == 0)
        {
            lv--;
        }

        if (lv < 0)
        {
            return;
        }

        var node = Root.ToReduce[lv].Last();
        Root.ToReduce[lv].Remove(node);

        // Merge children
        node.IsLeaf = true;
        node.r = 0;
        node.g = 0;
        node.b = 0;
        node.ChildrenCount = 0;
        for (var i = 0; i < 8; i++)
        {
            if (node.Children[i] == null)
            {
                continue;
            }
            var child = node.Children[i]!;
            node.r += child.r;
            node.g += child.g;
            node.b += child.b;
            node.ChildrenCount += child.ChildrenCount;
            Root.LeafNum--;
        }

        Root.LeafNum++;
    }

    public static void ColorStats(ColorOctTreeNode node, Dictionary<string, int> record)
    {
        if (node.IsLeaf)
        {
            var r = Convert.ToString(~~(node.r / node.ChildrenCount), 16).PadLeft(2, '0')!;
            var g = Convert.ToString(~~(node.g / node.ChildrenCount), 16).PadLeft(2, '0')!;
            var b = Convert.ToString(~~(node.b / node.ChildrenCount), 16).PadLeft(2, '0')!;

            var color = $"#{r}{g}{b}";
            if (record.Keys.Contains(color))
            {
                record[color] += node.ChildrenCount;
            }
            else
            {
                record[color] = node.ChildrenCount;
            }

            return;
        }

        for (var i = 0; i < 8; i++)
        {
            if (node.Children[i] != null)
            {
                ColorStats(node.Children[i]!, record);
            }
        }
    }

    public static unsafe Dictionary<string, int> ProcessImage(Bitmap img)
    {
        var root = new ColorOctTreeNode(null, 0, 0);
        
        var rect = new Rectangle(0, 0, img.Width, img.Height);
        var data = img.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, img.PixelFormat);
        try
        {
            var pixelSize = System.Drawing.Image.GetPixelFormatSize(img.PixelFormat) / 8;
            var stride = data.Stride;
            var ptr = data.Scan0;
            
            var maxPixels = 50000;
            var totalPixels = img.Width * img.Height;
            var step = totalPixels > maxPixels ? (int)Math.Ceiling((double)totalPixels / maxPixels) : 1;
            
            int processed = 0;
            int pixelsAdded = 0;
            
            for (int y = 0; y < img.Height; y++)
            {
                var row = (byte*)(ptr + y * stride);
                for (int x = 0; x < img.Width; x++)
                {
                    if (processed % step == 0)
                    {
                        var idx = x * pixelSize;
                        byte blue = row[idx];
                        byte green = row[idx + 1];
                        byte red = row[idx + 2];
                        var color = Color.FromArgb(red, green, blue);
                        
                        root.AddColor(color, 0);
                        pixelsAdded++;
                        while (root.LeafNum > 16) 
                        {
                            root.ReduceTree();
                        }
                    }
                    processed++;
                }
            }
        }
        finally
        {
            img.UnlockBits(data);
        }
        
        var r = new Dictionary<string, int>();
        ColorStats(root, r);
        return r;
    }
}