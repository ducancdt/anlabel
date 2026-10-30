"""Generates src/ANLAbel.App/Themes/RibbonIcons.xaml: ANLAbel's own flat icon family.

Run from the repository root:
    python scripts/make_ribbon_icons.py src/ANLAbel.App/Themes/RibbonIcons.xaml

Grid: large icons 32 x 32 DIP, small icons 16 x 16 DIP. One palette, 1 DIP outlines on
half-pixel coordinates so 100 % DPI stays crisp. Every icon starts with a transparent
square, so each DrawingImage measures exactly 32 or 16 whatever its content.
"""
import math
import os
import sys

OUT = sys.argv[1] if len(sys.argv) > 1 else 'RibbonIcons.xaml'

INK = '#3D4B5F'
INK_SOFT = '#6B7A90'
PAPER = '#FFFFFF'
LINE = '#8C9AAF'
BLUE = '#2D6CC0'
BLUE_D = '#1F4F91'
BLUE_M = '#5B93DA'
BLUE_L = '#D3E4FA'
GREEN = '#21804A'
GREEN_M = '#3FA567'
GREEN_L = '#D2EEDD'
GREEN_LINE = '#8CC7A3'
PURPLE = '#7451AE'
PURPLE_M = '#9A79CF'
PURPLE_L = '#E8DEF7'
ORANGE = '#D9821C'
ORANGE_L = '#FCE4C4'
RED = '#C93A3A'
RED_L = '#F8D7D7'
YELLOW = '#E2A93A'
YELLOW_D = '#B07F1C'
YELLOW_L = '#F8E1A0'
GRAY = '#8D99AB'
GRAY_D = '#5D6A7D'
GRAY_M = '#B7C1CF'
GRAY_L = '#E5EAF1'
TAN = '#A8793C'
TAN_L = '#EBD3AE'
MAGENTA = '#C8338A'


def f(v):
    s = ('%.2f' % v).rstrip('0').rstrip('.')
    return '0' if s in ('-0', '') else s


def rect(x0, y0, x1, y1):
    return f'M{f(x0)},{f(y0)} H{f(x1)} V{f(y1)} H{f(x0)} Z'


def rrect(x0, y0, x1, y1, r):
    return (f'M{f(x0 + r)},{f(y0)} H{f(x1 - r)} A{f(r)},{f(r)} 0 0 1 {f(x1)},{f(y0 + r)} '
            f'V{f(y1 - r)} A{f(r)},{f(r)} 0 0 1 {f(x1 - r)},{f(y1)} H{f(x0 + r)} '
            f'A{f(r)},{f(r)} 0 0 1 {f(x0)},{f(y1 - r)} V{f(y0 + r)} A{f(r)},{f(r)} 0 0 1 {f(x0 + r)},{f(y0)} Z')


def ellipse(cx, cy, rx, ry):
    return (f'M{f(cx - rx)},{f(cy)} A{f(rx)},{f(ry)} 0 1 1 {f(cx + rx)},{f(cy)} '
            f'A{f(rx)},{f(ry)} 0 1 1 {f(cx - rx)},{f(cy)} Z')


def circle(cx, cy, r):
    return ellipse(cx, cy, r, r)


def line(x0, y0, x1, y1):
    return f'M{f(x0)},{f(y0)} L{f(x1)},{f(y1)}'


def poly(points, closed=True):
    head = f'M{f(points[0][0])},{f(points[0][1])}'
    rest = ' '.join(f'L{f(x)},{f(y)}' for x, y in points[1:])
    return head + ' ' + rest + (' Z' if closed else '')


def page(x0, y0, x1, y1, fold):
    outline = f'M{f(x0)},{f(y0)} H{f(x1 - fold)} L{f(x1)},{f(y0 + fold)} V{f(y1)} H{f(x0)} Z'
    corner = f'M{f(x1 - fold)},{f(y0)} V{f(y0 + fold)} H{f(x1)} Z'
    return outline, corner


def cylinder(x0, x1, top, bottom, ry):
    rx = (x1 - x0) / 2
    cx = (x0 + x1) / 2
    body = (f'M{f(x0)},{f(top)} V{f(bottom)} A{f(rx)},{f(ry)} 0 0 0 {f(x1)},{f(bottom)} '
            f'V{f(top)} A{f(rx)},{f(ry)} 0 0 0 {f(x0)},{f(top)} Z')
    cap = ellipse(cx, top, rx, ry)
    return body, cap, rx


def ring(x0, x1, y, ry):
    rx = (x1 - x0) / 2
    return f'M{f(x0)},{f(y)} A{f(rx)},{f(ry)} 0 0 0 {f(x1)},{f(y)}'


def gear(cx, cy, ro, ri, teeth, hole):
    pts = []
    step = 2 * math.pi / teeth
    for i in range(teeth):
        a = i * step - math.pi / 2
        for da, r in ((-0.5, ri), (-0.28, ro), (0.28, ro), (0.5, ri)):
            ang = a + da * step
            pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    return 'F0 ' + poly(pts) + ' ' + circle(cx, cy, hole)


def arc_arrow(cx, cy, r, a0, a1, head):
    """Arc from angle a0 to a1 (degrees, screen coordinates, clockwise positive) and an
    arrow head at a1 pointing along the direction of travel."""
    def pt(a):
        rad = math.radians(a)
        return cx + r * math.cos(rad), cy + r * math.sin(rad)
    x0, y0 = pt(a0)
    x1, y1 = pt(a1)
    sweep = 1 if a1 > a0 else 0
    large = 1 if abs(a1 - a0) > 180 else 0
    arc = f'M{f(x0)},{f(y0)} A{f(r)},{f(r)} 0 {large} {sweep} {f(x1)},{f(y1)}'
    # tangent direction at a1
    rad = math.radians(a1)
    tx, ty = -math.sin(rad), math.cos(rad)
    if sweep == 0:
        tx, ty = -tx, -ty
    nx, ny = math.cos(rad), math.sin(rad)
    tip = (x1 + tx * head, y1 + ty * head)
    left = (x1 + nx * head * 0.8, y1 + ny * head * 0.8)
    right = (x1 - nx * head * 0.8, y1 - ny * head * 0.8)
    return arc, poly([tip, left, right])


def pencil(tip_x, tip_y, length, width, angle_deg):
    """Pencil lying from its tip along angle_deg (degrees, screen coordinates)."""
    a = math.radians(angle_deg)
    dx, dy = math.cos(a), math.sin(a)
    nx, ny = -dy, dx
    hw = width / 2
    cone = width * 1.1

    def p(along, side):
        return (tip_x + dx * along + nx * side, tip_y + dy * along + ny * side)
    tip = poly([p(0, 0), p(cone, hw), p(cone, -hw)])
    lead = poly([p(0, 0), p(cone * 0.42, hw * 0.42), p(cone * 0.42, -hw * 0.42)])
    body = poly([p(cone, hw), p(length - width * 0.9, hw), p(length - width * 0.9, -hw), p(cone, -hw)])
    eraser = poly([p(length - width * 0.9, hw), p(length, hw), p(length, -hw), p(length - width * 0.9, -hw)])
    return tip, lead, body, eraser


class Pen:
    def __init__(self, brush, thickness=1.0, dash=None, cap=None, join=None):
        self.brush, self.thickness, self.dash, self.cap, self.join = brush, thickness, dash, cap, join

    def xml(self):
        attrs = f'Brush="{self.brush}" Thickness="{f(self.thickness)}"'
        if self.cap:
            attrs += f' StartLineCap="{self.cap}" EndLineCap="{self.cap}"'
        if self.join:
            attrs += f' LineJoin="{self.join}"'
        if self.dash:
            return (f'<GeometryDrawing.Pen><Pen {attrs}><Pen.DashStyle><DashStyle Dashes="{self.dash}"/>'
                    f'</Pen.DashStyle></Pen></GeometryDrawing.Pen>')
        return f'<GeometryDrawing.Pen><Pen {attrs}/></GeometryDrawing.Pen>'


def shape(geometry, fill=None, pen=None):
    brush = f' Brush="{fill}"' if fill else ''
    if pen is None:
        return f'<GeometryDrawing{brush} Geometry="{geometry}"/>'
    return f'<GeometryDrawing{brush} Geometry="{geometry}">{pen.xml()}</GeometryDrawing>'


def stroke(geometry, color, thickness=1.0, **kw):
    return shape(geometry, None, Pen(color, thickness, **kw))


def clipped(clip, parts):
    return f'<DrawingGroup ClipGeometry="{clip}">' + ''.join(parts) + '</DrawingGroup>'


ICONS = {}


def icon(name, size):
    def register(fn):
        ICONS[name] = (size, fn)
        return fn
    return register


P1 = Pen(INK)

# ---------------------------------------------------------------- shared pieces


def printer(x, y, s, paper_lines=True, led=GREEN_M):
    """Printer whose body spans x..x+26*s, y..y+26*s (designed on a 26 unit box)."""
    def X(v): return x + v * s
    def Y(v): return y + v * s
    out = [
        shape(rect(X(5.5), Y(0.5), X(20.5), Y(8.5)), PAPER, P1),
        shape(rrect(X(0.5), Y(7.5), X(25.5), Y(19.5), 1.5 * s), GRAY_L, P1),
        stroke(line(X(3.5), Y(16.5), X(22.5), Y(16.5)), INK_SOFT),
        shape(rect(X(5.5), Y(13.5), X(20.5), Y(25.5)), PAPER, P1),
    ]
    if paper_lines:
        out += [stroke(line(X(8), Y(18.5), X(18), Y(18.5)), LINE), stroke(line(X(8), Y(21.5), X(15), Y(21.5)), LINE)]
    if led:
        out.append(shape(rect(X(20.5), Y(10), X(23), Y(12)), led))
    return out


def small_printer(lines=True):
    out = [
        shape(rect(4.5, 1.5, 11.5, 5.5), PAPER, P1),
        shape(rrect(1.5, 5.5, 14.5, 11.5, 1), GRAY_L, P1),
        shape(rect(4.5, 9.5, 11.5, 14.5), PAPER, P1),
    ]
    if lines:
        out.append(stroke(line(6, 12, 10, 12), LINE))
    return out


def db(x0, x1, top, bottom, ry, fill=GREEN_L, edge=GREEN, cap_fill=GREEN_M, rings=(1, 2)):
    body, cap, rx = cylinder(x0, x1, top, bottom, ry)
    out = [shape(body, fill, Pen(edge))]
    span = (bottom - top) / 3
    for k in rings:
        out.append(stroke(ring(x0, x1, top + span * k, ry), edge))
    out.append(shape(cap, cap_fill, Pen(edge)))
    return out


def brackets(c=BLUE, t=1.5):
    return [stroke('M3.5,10 V3.5 H10 M22,3.5 H28.5 V10 M28.5,22 V28.5 H22 M10,28.5 H3.5 V22', c, t, join='Miter')]


def badge_sliders(x0, y0, x1, y1):
    return [
        shape(rrect(x0, y0, x1, y1, 2), BLUE_L, Pen(BLUE)),
        stroke(line(x0 + 2.5, y0 + 4, x1 - 2.5, y0 + 4), BLUE),
        stroke(line(x0 + 2.5, y1 - 4, x1 - 2.5, y1 - 4), BLUE),
        shape(rect(x0 + 4.5, y0 + 2.5, x0 + 7.5, y0 + 5.5), BLUE),
        shape(rect(x1 - 7.5, y1 - 5.5, x1 - 4.5, y1 - 2.5), BLUE),
    ]


def badge_x(cx, cy, r):
    d = r * 0.42
    return [
        shape(circle(cx, cy, r), RED, Pen(PAPER)),
        stroke(f'M{f(cx - d)},{f(cy - d)} L{f(cx + d)},{f(cy + d)} M{f(cx + d)},{f(cy - d)} L{f(cx - d)},{f(cy + d)}', PAPER, 1.6, cap='Round'),
    ]


def badge_refresh(cx, cy, r, color=BLUE):
    a1, h1 = arc_arrow(cx, cy, r - 2.2, 200, 330, 2.2)
    a2, h2 = arc_arrow(cx, cy, r - 2.2, 20, 150, 2.2)
    return [
        shape(circle(cx, cy, r), PAPER, Pen(color)),
        stroke(a1, color, 1.3), shape(h1, color),
        stroke(a2, color, 1.3), shape(h2, color),
    ]


def bars(x0, y0, y1, pattern, color=INK):
    out = []
    x = x0
    for w, gap in pattern:
        out.append(shape(rect(x, y0, x + w, y1), color))
        x += w + gap
    return out


BARCODE = [(1.5, 1), (1, 1), (2.5, 1), (1, 2), (1.5, 1), (1, 1), (2.5, 1.5), (1, 1), (1.5, 1), (2, 1), (1, 1), (1.5, 0)]

# ---------------------------------------------------------------- Home: clipboard


@icon('Paste32', 32)
def _():
    return [
        shape(rrect(5.5, 4.5, 23.5, 28.5, 1.5), TAN_L, Pen(TAN)),
        shape(rrect(10.5, 2.5, 18.5, 7.5, 1), GRAY_M, P1),
        shape(rect(13.5, 11.5, 27.5, 29.5), PAPER, P1),
        stroke('M16,15.5 H25 M16,18.5 H25 M16,21.5 H25 M16,24.5 H22', LINE),
    ]


@icon('Cut16', 16)
def _():
    return [
        stroke('M10.8,1.2 L5.6,9.6 M5.2,1.2 L10.4,9.6', GRAY_D, 1.3, cap='Round'),
        stroke(circle(4.6, 12.2, 2.4), RED, 1.3),
        stroke(circle(11.4, 12.2, 2.4), RED, 1.3),
    ]


@icon('Copy16', 16)
def _():
    return [
        shape(rect(1.5, 1.5, 9.5, 11.5), PAPER, Pen(INK_SOFT)),
        shape(rect(5.5, 4.5, 13.5, 14.5), PAPER, Pen(BLUE)),
        stroke('M7.5,7.5 H11.5 M7.5,9.5 H11.5 M7.5,11.5 H10', BLUE_M),
    ]


@icon('FormatPainter16', 16)
def _():
    return [
        shape(rrect(1.5, 1.5, 12.5, 6.5, 1), YELLOW_L, Pen(YELLOW_D)),
        shape(rect(13, 3, 14.5, 9.5), GRAY_D),
        shape(rect(8, 8.5, 14.5, 9.8), GRAY_D),
        shape(rect(7.2, 9, 9.3, 15), TAN),
    ]

# ---------------------------------------------------------------- Home: font


@icon('GrowFont16', 16)
def _():
    return [
        stroke('M1.5,14 L5.5,3 L9.5,14 M3,10 H8', INK, 1.4, join='Round'),
        stroke('M10.5,6.5 L12.5,3.5 L14.5,6.5', BLUE, 1.4, cap='Round', join='Round'),
    ]


@icon('ShrinkFont16', 16)
def _():
    return [
        stroke('M2.5,14 L5.5,6 L8.5,14 M3.6,11.2 H7.4', INK, 1.3, join='Round'),
        stroke('M10.5,3.5 L12.5,6.5 L14.5,3.5', BLUE, 1.4, cap='Round', join='Round'),
    ]


def paragraph(lines):
    return [stroke(' '.join(f'M{f(a)},{f(y)} H{f(b)}' for a, b, y in lines), INK, 1.5)]


@icon('TextAlignLeft16', 16)
def _():
    return paragraph([(2, 14, 3.5), (2, 10, 6.5), (2, 14, 9.5), (2, 10, 12.5)])


@icon('TextAlignCenter16', 16)
def _():
    return paragraph([(2, 14, 3.5), (4, 12, 6.5), (2, 14, 9.5), (4, 12, 12.5)])


@icon('TextAlignRight16', 16)
def _():
    return paragraph([(2, 14, 3.5), (6, 14, 6.5), (2, 14, 9.5), (6, 14, 12.5)])


@icon('TextJustify16', 16)
def _():
    return paragraph([(2, 14, 3.5), (2, 14, 6.5), (2, 14, 9.5), (2, 14, 12.5)])

# ---------------------------------------------------------------- Home: action


@icon('Print32', 32)
def _():
    return printer(3, 3, 1)


@icon('PrintOptions32', 32)
def _():
    return printer(1, 6, 0.85) + badge_sliders(17.5, 1.5, 30.5, 14.5)


@icon('PrintCurrent16', 16)
def _():
    return small_printer(False) + [shape(rect(6, 11, 10, 13), BLUE)]


@icon('PrintAll16', 16)
def _():
    return [
        shape(rect(4.5, 1.5, 11.5, 5.5), PAPER, P1),
        shape(rrect(1.5, 5.5, 14.5, 11.5, 1), GRAY_L, P1),
        shape(rect(3.5, 8.5, 10.5, 13.5), PAPER, P1),
        shape(rect(5.5, 10.5, 12.5, 15.5), PAPER, Pen(BLUE)),
    ]


@icon('TestPrint16', 16)
def _():
    return small_printer(False) + [
        shape(circle(12, 12, 3.6), PAPER, Pen(ORANGE)),
        stroke('M12,9.2 V14.8 M9.2,12 H14.8', ORANGE),
    ]


@icon('PrintHistory16', 16)
def _():
    return [
        shape(circle(8, 8, 6.5), PAPER, Pen(BLUE)),
        stroke('M8,4 V8 L10.8,9.8', BLUE, 1.3, cap='Round', join='Round'),
        stroke('M1.5,3.5 V6 H4', BLUE, 1.2),
    ]


@icon('ExportExcel16', 16)
def _():
    return [
        shape(rect(1.5, 2.5, 11.5, 14.5), PAPER, Pen(GREEN)),
        shape(rect(1.5, 2.5, 11.5, 5.5), GREEN_M, Pen(GREEN)),
        stroke('M5.5,5.5 V14.5 M1.5,8.5 H11.5 M1.5,11.5 H11.5', GREEN_LINE),
        shape(poly([(9.5, 7.5), (15.5, 10.5), (9.5, 13.5)]), ORANGE),
    ]

# ---------------------------------------------------------------- Home: management


@icon('DataManager32', 32)
def _():
    return db(3.5, 19.5, 7.5, 24.5, 3, PURPLE_L, PURPLE, PURPLE_M) + [
        shape(rrect(14.5, 12.5, 30.5, 29.5, 2), PAPER, Pen(PURPLE)),
        stroke('M17.5,16.5 L21.5,21.5 M21.5,16.5 L17.5,21.5', PURPLE, 1.5, cap='Round'),
        stroke('M23.5,17.5 H27.5 M23.5,20.5 H27.5', PURPLE, 1.3),
        stroke('M17.5,25.5 H27.5', PURPLE_M),
    ]


@icon('DocumentProperties32', 32)
def _():
    outline, corner = page(5.5, 2.5, 22.5, 29.5, 5)
    return [
        shape(outline, PAPER, P1),
        shape(corner, GRAY_L, P1),
        stroke('M9,10.5 H17 M9,14.5 H19 M9,18.5 H14', LINE),
        shape(gear(22.5, 22.5, 7.8, 5.6, 8, 2.4), BLUE, Pen(PAPER)),
    ]


@icon('Variable16', 16)
def _():
    return [
        shape(rrect(1.5, 2.5, 14.5, 13.5, 1.5), PURPLE_L, Pen(PURPLE)),
        stroke('M4.5,5.5 L8,10.5 M8,5.5 L4.5,10.5', PURPLE, 1.3, cap='Round'),
        stroke('M9.5,7 H12.5 M9.5,9.5 H12.5', PURPLE, 1.1),
    ]


@icon('Counter16', 16)
def _():
    return [
        shape(rect(1.5, 9.5, 4.5, 14.5), PURPLE_L, Pen(PURPLE)),
        shape(rect(6.5, 6.5, 9.5, 14.5), PURPLE_L, Pen(PURPLE)),
        shape(rect(11.5, 2.5, 14.5, 14.5), PURPLE_M, Pen(PURPLE)),
    ]


@icon('Function16', 16)
def _():
    return [
        stroke('M6.5,14.5 V5 C6.5,2.4 8.4,1.8 10,2.3 M4.2,7.2 H9.2', PURPLE, 1.4, cap='Round'),
        stroke('M9.5,8.5 L14,14.5 M14,8.5 L9.5,14.5', PURPLE, 1.4, cap='Round'),
    ]


@icon('Database16', 16)
def _():
    return db(2.5, 13.5, 4, 12.5, 2, rings=(1.5,))


@icon('DateTime16', 16)
def _():
    return [
        shape(circle(8, 8, 6.5), PAPER, Pen(PURPLE)),
        stroke('M8,4.2 V8 L10.6,9.6', PURPLE, 1.3, cap='Round', join='Round'),
    ]

# ---------------------------------------------------------------- Home: align (objects)


def align(bar, blocks):
    return [shape(bar, INK)] + [shape(b, BLUE_L, Pen(BLUE)) for b in blocks]


@icon('AlignLeft16', 16)
def _():
    return align(rect(1, 1, 2.5, 15), [rect(3.5, 3.5, 13.5, 6.5), rect(3.5, 9.5, 9.5, 12.5)])


@icon('AlignCenterH16', 16)
def _():
    return [shape(rect(2.5, 3.5, 13.5, 6.5), BLUE_L, Pen(BLUE)), shape(rect(4.5, 9.5, 11.5, 12.5), BLUE_L, Pen(BLUE)),
            shape(rect(7.25, 1, 8.75, 15), INK)]


@icon('AlignRight16', 16)
def _():
    return align(rect(13.5, 1, 15, 15), [rect(2.5, 3.5, 12.5, 6.5), rect(6.5, 9.5, 12.5, 12.5)])


@icon('AlignTop16', 16)
def _():
    return align(rect(1, 1, 15, 2.5), [rect(3.5, 3.5, 6.5, 13.5), rect(9.5, 3.5, 12.5, 9.5)])


@icon('AlignCenterV16', 16)
def _():
    return [shape(rect(3.5, 2.5, 6.5, 13.5), BLUE_L, Pen(BLUE)), shape(rect(9.5, 4.5, 12.5, 11.5), BLUE_L, Pen(BLUE)),
            shape(rect(1, 7.25, 15, 8.75), INK)]


@icon('AlignBottom16', 16)
def _():
    return align(rect(1, 13.5, 15, 15), [rect(3.5, 2.5, 6.5, 12.5), rect(9.5, 6.5, 12.5, 12.5)])


@icon('DistributeH16', 16)
def _():
    return [shape(rect(1, 1, 2.5, 15), INK), shape(rect(13.5, 1, 15, 15), INK),
            shape(rect(5.5, 4.5, 10.5, 11.5), BLUE_L, Pen(BLUE)),
            stroke('M3.5,8 H5 M11,8 H12.5', BLUE)]


@icon('DistributeV16', 16)
def _():
    return [shape(rect(1, 1, 15, 2.5), INK), shape(rect(1, 13.5, 15, 15), INK),
            shape(rect(4.5, 5.5, 11.5, 10.5), BLUE_L, Pen(BLUE)),
            stroke('M8,3.5 V5 M8,11 V12.5', BLUE)]

# ---------------------------------------------------------------- Home: objects / arrange


@icon('GroupObjects32', 32)
def _():
    corners = [(3.5, 3.5), (28.5, 3.5), (28.5, 28.5), (3.5, 28.5)]
    return ([stroke(rect(3.5, 3.5, 28.5, 28.5), INK_SOFT, 1, dash='2,2'),
             shape(rect(7.5, 7.5, 18.5, 17.5), BLUE_L, Pen(BLUE)),
             shape(ellipse(19.5, 20.5, 6, 5), ORANGE_L, Pen(ORANGE))]
            + [shape(rect(x - 1.5, y - 1.5, x + 1.5, y + 1.5), PAPER, P1) for x, y in corners])


@icon('Group16', 16)
def _():
    return [stroke(rect(1.5, 1.5, 14.5, 14.5), INK_SOFT, 1, dash='1,1'),
            shape(rect(3.5, 3.5, 8.5, 8.5), BLUE_L, Pen(BLUE)),
            shape(circle(10.5, 10.5, 2.8), ORANGE_L, Pen(ORANGE))]


@icon('Ungroup16', 16)
def _():
    return [shape(rect(1.5, 1.5, 7.5, 7.5), BLUE_L, Pen(BLUE)),
            shape(circle(11.5, 11.5, 3), ORANGE_L, Pen(ORANGE)),
            stroke('M10,2.5 L13.5,6 M13.5,2.5 L10,6', RED, 1.2, cap='Round')]


@icon('Arrange32', 32)
def _():
    return [shape(rect(3.5, 3.5, 17.5, 15.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(9.5, 9.5, 23.5, 21.5), BLUE_L, Pen(BLUE)),
            shape(rect(15.5, 16.5, 29.5, 28.5), BLUE, Pen(BLUE_D))]


@icon('BringForward32', 32)
def _():
    return [shape(rect(3.5, 3.5, 19.5, 19.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(11.5, 11.5, 28.5, 28.5), BLUE, Pen(BLUE_D))]


@icon('SendBackward32', 32)
def _():
    return [shape(rect(3.5, 3.5, 19.5, 19.5), BLUE, Pen(BLUE_D)),
            shape(rect(11.5, 11.5, 28.5, 28.5), GRAY_L, Pen(GRAY_D))]


@icon('BringToFront16', 16)
def _():
    return [shape(rect(1.5, 1.5, 7.5, 7.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(8.5, 8.5, 14.5, 14.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(4.5, 4.5, 11.5, 11.5), BLUE, Pen(BLUE_D))]


@icon('BringForward16', 16)
def _():
    return [shape(rect(1.5, 1.5, 9.5, 9.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(6.5, 6.5, 14.5, 14.5), BLUE, Pen(BLUE_D))]


@icon('SendBackward16', 16)
def _():
    return [shape(rect(1.5, 1.5, 9.5, 9.5), BLUE, Pen(BLUE_D)),
            shape(rect(6.5, 6.5, 14.5, 14.5), GRAY_L, Pen(GRAY_D))]


@icon('SendToBack16', 16)
def _():
    return [shape(rect(4.5, 4.5, 11.5, 11.5), BLUE, Pen(BLUE_D)),
            shape(rect(1.5, 1.5, 7.5, 7.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(8.5, 8.5, 14.5, 14.5), GRAY_L, Pen(GRAY_D))]


@icon('Delete32', 32)
def _():
    return [stroke('M8.5,8.5 L23.5,23.5 M23.5,8.5 L8.5,23.5', RED, 3.4, cap='Round')]


@icon('Delete16', 16)
def _():
    return [stroke('M4,4 L12,12 M12,4 L4,12', RED, 2, cap='Round')]

# ---------------------------------------------------------------- Data tab


@icon('Excel32', 32)
def _():
    return [shape(rect(3.5, 3.5, 28.5, 28.5), PAPER, Pen(GREEN)),
            shape(rect(3.5, 8.5, 11.5, 28.5), GREEN_L),
            shape(rect(3.5, 3.5, 28.5, 8.5), GREEN_M, Pen(GREEN)),
            stroke('M11.5,8.5 V28.5 M20,8.5 V28.5 M3.5,13.5 H28.5 M3.5,18.5 H28.5 M3.5,23.5 H28.5', GREEN_LINE),
            stroke(rect(3.5, 3.5, 28.5, 28.5), GREEN)]


@icon('TextFile32', 32)
def _():
    outline, corner = page(6.5, 2.5, 25.5, 29.5, 6)
    return [shape(outline, PAPER, Pen(BLUE)), shape(corner, BLUE_L, Pen(BLUE)),
            stroke('M10,12.5 H16 M18,12.5 H22 M10,16.5 H14 M16,16.5 H22 M10,20.5 H17 M19,20.5 H22 M10,24.5 H15', LINE, 1.2),
            shape(rect(16.4, 11.8, 17.4, 13.2), BLUE), shape(rect(14.4, 15.8, 15.4, 17.2), BLUE), shape(rect(17.4, 19.8, 18.4, 21.2), BLUE)]


@icon('EditDatabase32', 32)
def _():
    tip, lead, body, eraser = pencil(15.5, 29, 17, 4.6, -45)
    return db(3.5, 21.5, 6.5, 23.5, 3.2) + [
        shape(body, YELLOW, Pen(YELLOW_D, 0.8)), shape(eraser, RED_L, Pen(RED, 0.8)),
        shape(tip, TAN_L, Pen(TAN, 0.8)), shape(lead, INK)]


@icon('RefreshDatabase32', 32)
def _():
    return db(3.5, 21.5, 6.5, 23.5, 3.2) + badge_refresh(23, 23, 7.5)


@icon('DeleteDatabase32', 32)
def _():
    return db(3.5, 21.5, 6.5, 23.5, 3.2) + badge_x(23.5, 23.5, 6.5)


@icon('ImportDataSources32', 32)
def _():
    return [
        shape(poly([(3.5, 17.5), (9.5, 17.5), (12.5, 21.5), (19.5, 21.5), (22.5, 17.5), (28.5, 17.5), (28.5, 28.5), (3.5, 28.5)]), PURPLE_L, Pen(PURPLE)),
        shape(poly([(13.5, 2.5), (18.5, 2.5), (18.5, 11.5), (23.5, 11.5), (16, 19.5), (8.5, 11.5), (13.5, 11.5)]), PURPLE, Pen(PURPLE, 1, join='Round')),
    ]


@icon('PromptOrder32', 32)
def _():
    rows = []
    for y in (3.5, 12.5, 21.5):
        rows += [shape(rrect(2.5, y, 20.5, y + 7, 1.5), PAPER, Pen(PURPLE)), shape(rect(5, y + 2.5, 7, y + 4.5), PURPLE),
                 stroke(f'M9,{f(y + 3.5)} H17.5', PURPLE_M)]
    return rows + [
        stroke('M26,7.5 V24.5', PURPLE, 1.6),
        shape(poly([(26, 2.5), (30.5, 9), (21.5, 9)]), PURPLE),
        shape(poly([(26, 29.5), (30.5, 23), (21.5, 23)]), PURPLE),
    ]

# ---------------------------------------------------------------- View tab


@icon('ZoomToDocument32', 32)
def _():
    outline, corner = page(9.5, 7.5, 22.5, 24.5, 4)
    return brackets() + [shape(outline, PAPER, P1), shape(corner, GRAY_L, P1), stroke('M12,14.5 H19 M12,17.5 H19 M12,20.5 H16', LINE)]


@icon('ZoomToObjects32', 32)
def _():
    return brackets() + [shape(rect(8.5, 8.5, 17.5, 17.5), BLUE_L, Pen(BLUE)), shape(circle(19.5, 19.5, 4.5), ORANGE_L, Pen(ORANGE))]


@icon('ZoomIn16', 16)
def _():
    return [shape(circle(6.5, 6.5, 4.8), PAPER, P1), stroke('M10,10 L14.5,14.5', INK, 2.2, cap='Round'),
            stroke('M4.2,6.5 H8.8 M6.5,4.2 V8.8', BLUE, 1.3)]


@icon('ZoomOut16', 16)
def _():
    return [shape(circle(6.5, 6.5, 4.8), PAPER, P1), stroke('M10,10 L14.5,14.5', INK, 2.2, cap='Round'),
            stroke('M4.2,6.5 H8.8', BLUE, 1.3)]


@icon('Zoom16', 16)
def _():
    return [shape(circle(6.5, 6.5, 4.8), BLUE_L, P1), stroke('M10,10 L14.5,14.5', INK, 2.2, cap='Round')]


@icon('Gridlines32', 32)
def _():
    return [shape(rect(3.5, 3.5, 28.5, 28.5), PAPER, P1),
            stroke('M9.75,3.5 V28.5 M16,3.5 V28.5 M22.25,3.5 V28.5 M3.5,9.75 H28.5 M3.5,16 H28.5 M3.5,22.25 H28.5', BLUE_M, 1, dash='1.5,1')]


@icon('SnapObjects16', 16)
def _():
    return [shape(rect(1.5, 1.5, 7.5, 7.5), BLUE_L, Pen(BLUE)), shape(rect(8.5, 7.5, 14.5, 13.5), BLUE_L, Pen(BLUE)),
            stroke('M0.5,7.5 H15.5', MAGENTA, 1, dash='2,1')]


@icon('SnapGrid16', 16)
def _():
    dots = [shape(rect(x - 0.6, y - 0.6, x + 0.6, y + 0.6), GRAY_D) for x in (2, 6, 10, 14) for y in (2, 6, 10, 14)]
    return dots + [shape(rect(5.5, 5.5, 10.5, 10.5), BLUE_L, Pen(BLUE))]


@icon('DoNotSnap16', 16)
def _():
    return [shape(rect(1.5, 1.5, 9.5, 9.5), BLUE_L, Pen(BLUE)), shape(circle(10.5, 10.5, 4.6), PAPER, Pen(RED, 1.4)),
            stroke('M7.3,13.7 L13.7,7.3', RED, 1.4)]


@icon('RotateView32', 32)
def _():
    outline, corner = page(10.5, 9.5, 25.5, 29.5, 4)
    arc, head = arc_arrow(17, 17, 12, 200, 290, 3.2)
    return [shape(outline, PAPER, P1), shape(corner, GRAY_L, P1), stroke('M13.5,17.5 H21 M13.5,21.5 H21 M13.5,25.5 H18', LINE),
            stroke(arc, BLUE, 2, cap='Round'), shape(head, BLUE)]


@icon('Panels32', 32)
def _():
    return [shape(rect(2.5, 4.5, 29.5, 27.5), PAPER, P1), shape(rect(2.5, 4.5, 29.5, 8.5), BLUE, P1),
            shape(rect(2.5, 8.5, 10.5, 27.5), BLUE_L, P1), shape(rect(21.5, 8.5, 29.5, 27.5), BLUE_L, P1),
            stroke('M4.5,12.5 H8.5 M4.5,15.5 H8.5 M23.5,12.5 H27.5 M23.5,15.5 H27.5', BLUE_M)]


@icon('ObjectMarkers16', 16)
def _():
    return [shape(poly([(1.5, 3.5), (10.5, 3.5), (14.5, 8), (10.5, 12.5), (1.5, 12.5)]), PURPLE_L, Pen(PURPLE)),
            shape(circle(10.5, 8, 1.3), PAPER, Pen(PURPLE))]

# ---------------------------------------------------------------- Help tab


@icon('Help32', 32)
def _():
    return [shape(circle(16, 16, 13), BLUE),
            stroke('M11.6,12.4 C11.6,6.8 20.6,6.8 20.6,12 C20.6,15.6 16,15.8 16,19.6', PAPER, 2.6, cap='Round'),
            shape(circle(16, 24, 1.8), PAPER)]


@icon('UserGuide32', 32)
def _():
    return [shape(poly([(2.5, 6.5), (15.5, 8.5), (15.5, 27.5), (2.5, 25.5)]), PAPER, Pen(BLUE)),
            shape(poly([(16.5, 8.5), (29.5, 6.5), (29.5, 25.5), (16.5, 27.5)]), PAPER, Pen(BLUE)),
            shape(rect(15.5, 8.5, 16.5, 27.5), BLUE),
            stroke('M5,12 L13,13.2 M5,15.5 L13,16.7 M5,19 L13,20.2 M19,13.2 L27,12 M19,16.7 L27,15.5 M19,20.2 L24,19.4', LINE)]


@icon('SampleFiles32', 32)
def _():
    return [shape('M3.5,7.5 H12.5 L15.5,10.5 H28.5 V26.5 H3.5 Z', YELLOW_L, Pen(YELLOW_D)),
            shape(rect(3.5, 13.5, 28.5, 26.5), YELLOW, Pen(YELLOW_D)),
            shape(rrect(13.5, 16.5, 26.5, 23.5, 1), PAPER, Pen(BLUE))]\
        + bars(15.5, 18, 22, [(1, 0.8), (0.6, 0.8), (1.4, 0.8), (0.6, 0.8), (1, 0.8), (0.6, 0.8), (1.4, 0)], INK)


@icon('Support32', 32)
def _():
    return [shape('M6.5,4.5 H25.5 A3,3 0 0 1 28.5,7.5 V18.5 A3,3 0 0 1 25.5,21.5 H14.5 L8.5,27.5 V21.5 H6.5 A3,3 0 0 1 3.5,18.5 V7.5 A3,3 0 0 1 6.5,4.5 Z',
                  BLUE_L, Pen(BLUE)),
            shape(circle(10.5, 13, 1.8), BLUE), shape(circle(16, 13, 1.8), BLUE), shape(circle(21.5, 13, 1.8), BLUE)]


@icon('About32', 32)
def _():
    return [shape(circle(16, 16, 12.5), BLUE_L, Pen(BLUE)), shape(circle(16, 9.8, 2), BLUE), shape(rect(14.3, 13.5, 17.7, 23.5), BLUE)]


@icon('HomePage32', 32)
def _():
    return [shape(circle(16, 16, 12.5), BLUE_L, Pen(BLUE)),
            stroke(ellipse(16, 16, 5.5, 12.5), BLUE),
            stroke('M3.5,16 H28.5 M5.8,9.5 H26.2 M5.8,22.5 H26.2 M16,3.5 V28.5', BLUE)]


@icon('CheckUpdates32', 32)
def _():
    return [shape(circle(16, 16, 12.5), GREEN_L, Pen(GREEN)),
            shape(poly([(14, 7.5), (18, 7.5), (18, 15.5), (22.5, 15.5), (16, 23), (9.5, 15.5), (14, 15.5)]), GREEN)]


@icon('Settings16', 16)
def _():
    return [shape(gear(8, 8, 7, 5, 8, 2.2), GRAY_D)]

# ---------------------------------------------------------------- Design (contextual)


@icon('VisibilitySettings16', 16)
def _():
    return [shape('M1,8 C3.8,3.2 12.2,3.2 15,8 C12.2,12.8 3.8,12.8 1,8 Z', PAPER, Pen(INK)),
            shape(circle(8, 8, 2.8), BLUE), shape(circle(8, 8, 1.1), INK)]


@icon('ObjectName32', 32)
def _():
    tip, lead, body, eraser = pencil(13, 22, 17, 4.4, -45)
    return [shape(rect(3.5, 8.5, 26.5, 26.5), PAPER, P1), stroke('M6.5,22.5 H13', LINE),
            shape(body, YELLOW, Pen(YELLOW_D, 0.8)), shape(eraser, RED_L, Pen(RED, 0.8)), shape(tip, TAN_L, Pen(TAN, 0.8)), shape(lead, INK)]


@icon('AnchorPoint32', 32)
def _():
    out = [stroke(rect(4.5, 4.5, 27.5, 27.5), INK_SOFT, 1, dash='2,2')]
    for x in (4.5, 16, 27.5):
        for y in (4.5, 16, 27.5):
            if (x, y) == (4.5, 4.5):
                continue
            out.append(shape(rect(x - 1.5, y - 1.5, x + 1.5, y + 1.5), PAPER, Pen(INK_SOFT)))
    out.append(shape(rect(1.5, 1.5, 7.5, 7.5), BLUE, Pen(BLUE_D)))
    return out

# ---------------------------------------------------------------- Barcode (contextual)


@icon('Barcode32', 32)
def _():
    return [shape(rect(2.5, 4.5, 29.5, 27.5), PAPER, P1)] + bars(5.5, 7.5, 20.5, BARCODE) + [stroke('M6,24.5 H26', INK_SOFT, 1.4, dash='1.5,1')]


@icon('BarcodeDetails32', 32)
def _():
    return bars(2.5, 3.5, 17.5, BARCODE[:9]) + [stroke('M3,21.5 H15', INK_SOFT, 1.4, dash='1.5,1')] + badge_sliders(15.5, 15.5, 30.5, 29.5)


@icon('Barcode2D32', 32)
def _():
    def finder(x, y):
        return [shape(rect(x + 0.5, y + 0.5, x + 8.5, y + 8.5), PAPER, Pen(INK, 1.6)), shape(rect(x + 3, y + 3, x + 6, y + 6), INK)]
    mods = [(13, 3), (16, 6), (13, 9), (19, 12), (4, 13), (10, 13), (13, 16), (16, 19), (22, 16), (25, 19), (19, 22), (13, 25), (25, 25), (16, 13), (7, 16)]
    return finder(2, 2) + finder(21, 2) + finder(2, 21) + [shape(rect(x, y, x + 3, y + 3), INK) for x, y in mods]


@icon('BearerBar32', 32)
def _():
    return [shape(rect(2, 4, 30, 7), INK), shape(rect(2, 25, 30, 28), INK)] + bars(5.5, 9, 23, BARCODE)


@icon('Color32', 32)
def _():
    return [shape('M16,3.5 C23.5,3.5 28.5,8.5 28.5,14.5 C28.5,19 25,20 22.5,20 C20,20 18.5,21.5 19.8,24 C21.2,26.8 19.5,28.5 16,28.5 C9,28.5 3.5,23 3.5,16 C3.5,9 9,3.5 16,3.5 Z',
                  TAN_L, Pen(TAN)),
            shape(circle(10, 12, 2.4), RED), shape(circle(16, 8.8, 2.4), YELLOW), shape(circle(22.2, 11.5, 2.4), GREEN_M), shape(circle(9.5, 19.5, 2.4), BLUE)]

# ---------------------------------------------------------------- Shape (contextual)


@icon('OutlineStyle32', 32)
def _():
    return [stroke('M3.5,7.5 H28.5', INK, 2.4), stroke('M3.5,16 H28.5', INK, 2.4, dash='2,1.2'), stroke('M3.5,24.5 H28.5', INK, 2.4, dash='0.6,1')]


@icon('OutlineColor32', 32)
def _():
    tip, lead, body, eraser = pencil(6, 22, 19, 4.6, -45)
    return [shape(body, YELLOW, Pen(YELLOW_D, 0.8)), shape(eraser, RED_L, Pen(RED, 0.8)), shape(tip, TAN_L, Pen(TAN, 0.8)), shape(lead, INK),
            shape(rect(2.5, 25.5, 29.5, 29.5), RED, Pen(INK_SOFT))]


@icon('FillStyle32', 32)
def _():
    hatch = ' '.join(f'M{f(x)},28.5 L{f(x + 25)},3.5' for x in (-21.5, -16.5, -11.5, -6.5, -1.5, 3.5, 8.5, 13.5, 18.5, 23.5))
    return [shape(rect(3.5, 3.5, 28.5, 28.5), PAPER), clipped(rect(3.5, 3.5, 28.5, 28.5), [stroke(hatch, BLUE_M, 1.4)]),
            stroke(rect(3.5, 3.5, 28.5, 28.5), BLUE)]


@icon('FillColor32', 32)
def _():
    return [shape('M5.5,13.5 L14,5 L23.5,14.5 L15,23 Z', GRAY_L, P1),
            stroke('M9.5,9.5 C7.5,7.5 7.5,4.5 10,3.5', INK, 1.2),
            shape('M24.5,14.5 C24.5,14.5 27.5,18.2 27.5,19.8 A3,3 0 0 1 21.5,19.8 C21.5,18.2 24.5,14.5 24.5,14.5 Z', BLUE),
            shape(rect(2.5, 25.5, 29.5, 29.5), YELLOW, Pen(INK_SOFT))]

# ---------------------------------------------------------------- Picture (contextual)


@icon('ReplacePicture32', 32)
def _():
    return [shape(rect(2.5, 4.5, 23.5, 22.5), BLUE_L, Pen(BLUE)),
            shape(poly([(4, 21), (10, 13), (14, 18), (17, 15), (22, 21)]), GREEN_M),
            shape(circle(17.5, 9.5, 2.3), YELLOW)] + badge_refresh(23.5, 23.5, 7)


@icon('KeepAspect16', 16)
def _():
    return [stroke(rect(1.5, 4.5, 14.5, 11.5), INK), stroke('M4,8 H12 M4,8 L6,6.2 M4,8 L6,9.8 M12,8 L10,6.2 M12,8 L10,9.8', BLUE, 1.1)]

# ---------------------------------------------------------------- Backstage / file


@icon('New32', 32)
def _():
    outline, corner = page(6.5, 2.5, 25.5, 29.5, 6)
    return [shape(outline, PAPER, P1), shape(corner, GRAY_L, P1),
            shape(circle(22, 24, 5.6), GREEN_M, Pen(PAPER)), stroke('M22,21 V27 M19,24 H25', PAPER, 1.6)]


@icon('Open32', 32)
def _():
    return [shape('M2.5,7.5 H11.5 L14.5,10.5 H25.5 V14.5 H2.5 Z', YELLOW_L, Pen(YELLOW_D)),
            shape(poly([(2.5, 26.5), (7.5, 14.5), (29.5, 14.5), (24.5, 26.5)]), YELLOW, Pen(YELLOW_D)),
            stroke('M2.5,7.5 V26.5', YELLOW_D)]


@icon('Save32', 32)
def _():
    return [shape(rrect(3.5, 3.5, 28.5, 28.5, 2), BLUE, Pen(BLUE_D)), shape(rect(8.5, 3.5, 23.5, 12.5), PAPER, Pen(BLUE_D)),
            shape(rect(18.5, 5.5, 21.5, 10.5), BLUE), shape(rect(7.5, 17.5, 24.5, 28.5), BLUE_L, Pen(BLUE_D))]


@icon('SaveAs32', 32)
def _():
    tip, lead, body, eraser = pencil(17, 30, 15, 4.2, -45)
    return [shape(rrect(2.5, 2.5, 24.5, 24.5, 2), BLUE, Pen(BLUE_D)), shape(rect(6.5, 2.5, 20.5, 10.5), PAPER, Pen(BLUE_D)),
            shape(rect(16, 4.5, 18.5, 8.5), BLUE), shape(rect(6, 15.5, 21, 24.5), BLUE_L, Pen(BLUE_D)),
            shape(body, YELLOW, Pen(YELLOW_D, 0.8)), shape(eraser, RED_L, Pen(RED, 0.8)), shape(tip, TAN_L, Pen(TAN, 0.8)), shape(lead, INK)]


@icon('Import32', 32)
def _():
    outline, corner = page(10.5, 2.5, 28.5, 29.5, 6)
    return [shape(outline, PAPER, P1), shape(corner, GRAY_L, P1),
            shape(poly([(2.5, 13.5), (11.5, 13.5), (11.5, 9.5), (19.5, 16), (11.5, 22.5), (11.5, 18.5), (2.5, 18.5)]), GREEN_M, Pen(GREEN))]


@icon('Close32', 32)
def _():
    outline, corner = page(5.5, 2.5, 24.5, 29.5, 6)
    return [shape(outline, PAPER, P1), shape(corner, GRAY_L, P1)] + badge_x(22.5, 23.5, 6.5)


@icon('Revisions32', 32)
def _():
    o1, c1 = page(9.5, 2.5, 26.5, 23.5, 5)
    return [shape(rect(3.5, 8.5, 20.5, 29.5), GRAY_L, Pen(INK_SOFT)), shape(o1, PAPER, P1), shape(c1, GRAY_L, P1),
            shape(circle(22.5, 23.5, 6.5), PAPER, Pen(BLUE)), stroke('M22.5,19.8 V23.5 L25,25', BLUE, 1.3, cap='Round')]


@icon('Options32', 32)
def _():
    return [shape(gear(16, 16, 13, 9.5, 10, 4.2), GRAY_D)]


@icon('Exit32', 32)
def _():
    return [shape(rect(4.5, 3.5, 18.5, 28.5), GRAY_L, P1), shape(poly([(4.5, 3.5), (14.5, 6.5), (14.5, 29.5), (4.5, 28.5)]), TAN_L, Pen(TAN)),
            shape(poly([(17.5, 13.5), (24.5, 13.5), (24.5, 9.5), (30.5, 16), (24.5, 22.5), (24.5, 18.5), (17.5, 18.5)]), RED)]


@icon('Templates32', 32)
def _():
    o1, c1 = page(3.5, 6.5, 19.5, 29.5, 4)
    o2, c2 = page(12.5, 2.5, 28.5, 25.5, 4)
    return [shape(o1, PAPER, Pen(INK_SOFT)), shape(c1, GRAY_L, Pen(INK_SOFT)), shape(o2, PAPER, Pen(BLUE)), shape(c2, BLUE_L, Pen(BLUE)),
            shape(rrect(15, 10, 26, 18, 1), BLUE_L, Pen(BLUE))] + bars(16.5, 12, 16, [(1, 0.8), (0.6, 0.8), (1.4, 0.8), (0.6, 0.8), (1, 0.8), (0.6, 0)], INK)


@icon('Align16', 16)
def _():
    return [stroke('M2.5,1 V15', MAGENTA, 1, dash='1.5,1'),
            shape(rect(3.5, 3.5, 13.5, 6.5), BLUE_L, Pen(BLUE)), shape(rect(3.5, 9.5, 9.5, 12.5), BLUE_L, Pen(BLUE))]


@icon('SharedSources32', 32)
def _():
    cards = []
    for x0 in (1.5, 18.5):
        cards += [shape(rrect(x0, 2.5, x0 + 12, 10.5, 1), PAPER, Pen(BLUE))]
        cards += bars(x0 + 2.5, 4.5, 8.5, [(1, 0.8), (0.6, 0.8), (1.4, 0.8), (0.6, 0.8), (1, 0.8), (0.6, 0)], INK)
    return cards + [stroke('M7.5,10.5 L12,15.5 M24.5,10.5 L20,15.5', GRAY_D, 1.2)] + db(9.5, 22.5, 16.5, 28.5, 2.6)


@icon('ZoomToDocument16', 16)
def _():
    return [stroke('M1.5,5 V1.5 H5 M11,1.5 H14.5 V5 M14.5,11 V14.5 H11 M5,14.5 H1.5 V11', BLUE, 1.2),
            shape(rect(5.5, 4.5, 10.5, 11.5), PAPER, P1)]


@icon('ZoomToObjects16', 16)
def _():
    return [stroke('M1.5,5 V1.5 H5 M11,1.5 H14.5 V5 M14.5,11 V14.5 H11 M5,14.5 H1.5 V11', BLUE, 1.2),
            shape(rect(4.5, 4.5, 8.5, 8.5), BLUE_L, Pen(BLUE)), shape(circle(10, 10, 2.2), ORANGE_L, Pen(ORANGE))]


@icon('Printer16', 16)
def _():
    return small_printer(True)


def plus_badge(cx, cy, r):
    d = r * 0.5
    return [shape(circle(cx, cy, r), GREEN_M, Pen(PAPER)),
            stroke(f'M{f(cx - d)},{f(cy)} H{f(cx + d)} M{f(cx)},{f(cy - d)} V{f(cy + d)}', PAPER, 1.6)]


@icon('AddVariable32', 32)
def _():
    return [shape(rrect(2.5, 5.5, 25.5, 24.5, 3), PURPLE_L, Pen(PURPLE)),
            stroke('M7.5,10.5 L13.5,19.5 M13.5,10.5 L7.5,19.5', PURPLE, 2, cap='Round'),
            stroke('M16.5,12.5 H22 M16.5,16.5 H22', PURPLE, 1.8)] + plus_badge(24.5, 24.5, 6.5)


@icon('AddFunction32', 32)
def _():
    return [stroke('M10.5,28 V11 C10.5,5.5 14.5,4.5 17.5,5.5 M6.5,13 H15.5', PURPLE, 2.4, cap='Round'),
            stroke('M15.5,15.5 L23,27 M23,15.5 L15.5,27', PURPLE, 2.4, cap='Round')] + plus_badge(25, 8, 6)


@icon('Counter32', 32)
def _():
    return [shape(rect(3.5, 18.5, 9.5, 28.5), PURPLE_L, Pen(PURPLE)),
            shape(rect(12.5, 12.5, 18.5, 28.5), PURPLE_L, Pen(PURPLE)),
            shape(rect(21.5, 5.5, 27.5, 28.5), PURPLE_M, Pen(PURPLE)),
            stroke('M4,10 L13,4.5', GREEN, 1.6, cap='Round'), shape(poly([(14.8, 3.2), (12.2, 7.4), (10.4, 3.4)]), GREEN)]


@icon('ArrowUp16', 16)
def _():
    return [shape(poly([(8, 2), (14, 9), (10, 9), (10, 14), (6, 14), (6, 9), (2, 9)]), BLUE, Pen(BLUE_D, 0.8, join='Round'))]


@icon('ArrowDown16', 16)
def _():
    return [shape(poly([(8, 14), (14, 7), (10, 7), (10, 2), (6, 2), (6, 7), (2, 7)]), BLUE, Pen(BLUE_D, 0.8, join='Round'))]


@icon('Text32', 32)
def _():
    return [shape(rect(6, 4.5, 26, 9), INK), shape(rect(13.8, 9, 18.2, 27.5), INK), shape(rect(10.5, 25.5, 21.5, 27.5), INK)]


@icon('TextBox32', 32)
def _():
    corners = [(3.5, 5.5), (28.5, 5.5), (28.5, 26.5), (3.5, 26.5)]
    return ([stroke(rect(3.5, 5.5, 28.5, 26.5), BLUE)]
            + [shape(rect(x - 1.5, y - 1.5, x + 1.5, y + 1.5), PAPER, Pen(BLUE)) for x, y in corners]
            + [shape(rect(10, 10, 22, 13), INK), shape(rect(14.4, 13, 17.6, 22.5), INK)])


@icon('RichText32', 32)
def _():
    return [stroke('M4,26.5 L11.5,5.5 L19,26.5 M6.8,19 H16.2', INK, 2.6, join='Round'),
            stroke(circle(24, 22.2, 3.6), RED, 2.2), stroke('M27.6,17.5 V26.8', RED, 2.2),
            shape(rect(3, 28.5, 29, 30), BLUE)]


@icon('Picture32', 32)
def _():
    return [shape(rect(3.5, 5.5, 28.5, 26.5), BLUE_L, Pen(BLUE)),
            shape(poly([(5, 25), (12, 15), (17, 21), (21, 17), (27, 25)]), GREEN_M),
            shape(circle(22, 10.5, 2.6), YELLOW)]


@icon('Rectangle32', 32)
def _():
    return [stroke(rect(4.5, 8.5, 27.5, 23.5), INK, 1.6)]


@icon('Line32', 32)
def _():
    return [stroke('M5,26 L27,6', INK, 2, cap='Round')]


@icon('Ellipse32', 32)
def _():
    return [stroke(ellipse(16, 16, 12, 8.5), INK, 1.6)]


@icon('Inverse32', 32)
def _():
    return [shape(rect(4.5, 7.5, 16, 24.5), INK), shape(rect(16, 7.5, 27.5, 24.5), PAPER),
            stroke(rect(4.5, 7.5, 27.5, 24.5), INK, 1.2),
            stroke('M7.5,12.5 H13.5 M7.5,16 H13.5 M7.5,19.5 H11.5', PAPER, 1.6),
            stroke('M18.5,12.5 H24.5 M18.5,16 H24.5 M18.5,19.5 H22.5', INK, 1.6)]


@icon('Layers16', 16)
def _():
    return [shape(poly([(8, 1.5), (14.5, 4.5), (8, 7.5), (1.5, 4.5)]), BLUE_L, Pen(BLUE, 1, join='Round')),
            stroke('M1.5,8 L8,11 L14.5,8 M1.5,11.5 L8,14.5 L14.5,11.5', BLUE, 1, join='Round')]


@icon('Objects16', 16)
def _():
    return [shape(rect(1.5, 1.5, 7.5, 7.5), BLUE_L, Pen(BLUE)), shape(circle(11.5, 4.5, 3), ORANGE_L, Pen(ORANGE)),
            shape(rect(1.5, 9.5, 14.5, 14.5), GRAY_L, Pen(GRAY_D))]


# ---------------------------------------------------------------- Small icons for panels, dialogs and the guide


def stadium(cx, cy, length, width, angle_deg):
    """A rounded bar (one chain link) centred on cx, cy and turned by angle_deg."""
    a = math.radians(angle_deg)
    dx, dy = math.cos(a), math.sin(a)
    nx, ny = -dy, dx
    half = (length - width) / 2
    r = width / 2
    p1 = (cx - dx * half + nx * r, cy - dy * half + ny * r)
    p2 = (cx + dx * half + nx * r, cy + dy * half + ny * r)
    p3 = (cx + dx * half - nx * r, cy + dy * half - ny * r)
    p4 = (cx - dx * half - nx * r, cy - dy * half - ny * r)
    return (f'M{f(p1[0])},{f(p1[1])} L{f(p2[0])},{f(p2[1])} A{f(r)},{f(r)} 0 0 0 {f(p3[0])},{f(p3[1])} '
            f'L{f(p4[0])},{f(p4[1])} A{f(r)},{f(r)} 0 0 0 {f(p1[0])},{f(p1[1])} Z')


@icon('Search16', 16)
def _():
    return [stroke(circle(6.5, 6.5, 4.5), INK, 1.5), stroke('M10,10 L14,14', INK, 2.2, cap='Round')]


@icon('Link16', 16)
def _():
    return [stroke(stadium(5.7, 10.3, 8.6, 4.6, -45), BLUE, 1.5), stroke(stadium(10.3, 5.7, 8.6, 4.6, -45), BLUE, 1.5)]


@icon('Unlink16', 16)
def _():
    return [stroke(stadium(4.6, 11.4, 6.4, 4.4, -45), BLUE, 1.5), stroke(stadium(11.4, 4.6, 6.4, 4.4, -45), BLUE, 1.5),
            stroke('M6.5,4.5 L6,2 M4.5,6.5 L2,6 M9.5,11.5 L10,14 M11.5,9.5 L14,10', RED, 1.2, cap='Round')]


@icon('Refresh16', 16)
def _():
    a1, h1 = arc_arrow(8, 8, 5.2, 200, 330, 2.5)
    a2, h2 = arc_arrow(8, 8, 5.2, 20, 150, 2.5)
    return [stroke(a1, BLUE, 1.5), shape(h1, BLUE), stroke(a2, BLUE, 1.5), shape(h2, BLUE)]


@icon('Excel16', 16)
def _():
    return [shape(rect(1.5, 1.5, 14.5, 14.5), PAPER, Pen(GREEN)),
            shape(rect(1.5, 1.5, 14.5, 4.5), GREEN_M, Pen(GREEN)),
            stroke('M5.5,4.5 V14.5 M9.5,4.5 V14.5 M1.5,8.5 H14.5 M1.5,11.5 H14.5', GREEN_LINE),
            stroke(rect(1.5, 1.5, 14.5, 14.5), GREEN)]


@icon('HomePage16', 16)
def _():
    return [shape(circle(8, 8, 6.5), BLUE_L, Pen(BLUE)), stroke(ellipse(8, 8, 2.8, 6.5), BLUE), stroke('M1.5,8 H14.5', BLUE)]


@icon('CheckUpdates16', 16)
def _():
    return [shape(circle(8, 8, 6.5), GREEN_L, Pen(GREEN)),
            shape(poly([(7, 3.5), (9, 3.5), (9, 8), (11.5, 8), (8, 12), (4.5, 8), (7, 8)]), GREEN)]


@icon('Folder16', 16)
def _():
    return [shape('M1.5,3.5 H6 L7.5,5 H14.5 V13.5 H1.5 Z', YELLOW_L, Pen(YELLOW_D)),
            shape(rect(1.5, 6.5, 14.5, 13.5), YELLOW, Pen(YELLOW_D))]


@icon('Panels16', 16)
def _():
    return [shape(rect(1.5, 2.5, 14.5, 13.5), PAPER, P1), shape(rect(1.5, 2.5, 14.5, 4.5), BLUE, P1),
            shape(rect(1.5, 4.5, 5.5, 13.5), BLUE_L, P1), shape(rect(10.5, 4.5, 14.5, 13.5), BLUE_L, P1)]


@icon('Pointer16', 16)
def _():
    return [shape(poly([(3.5, 1.5), (3.5, 13), (6.5, 10.3), (8.6, 14.6), (10.6, 13.7), (8.5, 9.4), (12.4, 9.4)]),
                  PAPER, Pen(INK, 1, join='Round'))]


@icon('ChevronUp16', 16)
def _():
    return [stroke('M4,10 L8,6 L12,10', INK_SOFT, 1.5, cap='Round', join='Round')]

# ---------------------------------------------------------------- Object types, 16 px (Properties header, guide)


@icon('Text16', 16)
def _():
    return [shape(rect(3, 2.5, 13, 4.5), INK), shape(rect(7, 4.5, 9, 13.5), INK), shape(rect(5.5, 12, 10.5, 13.5), INK)]


@icon('TextBox16', 16)
def _():
    corners = [(2.5, 3.5), (13.5, 3.5), (13.5, 12.5), (2.5, 12.5)]
    return ([stroke(rect(2.5, 3.5, 13.5, 12.5), BLUE)]
            + [shape(rect(x - 1, y - 1, x + 1, y + 1), PAPER, Pen(BLUE)) for x, y in corners]
            + [shape(rect(5, 5.5, 11, 7), INK), shape(rect(7.2, 7, 8.8, 10.5), INK)])


@icon('RichText16', 16)
def _():
    return [stroke('M2,13 L5.5,3 L9,13 M3.4,9.5 H7.6', INK, 1.4, join='Round'),
            stroke(circle(11.6, 10.6, 2), RED, 1.3), stroke('M13.6,8.2 V13', RED, 1.3),
            shape(rect(1.5, 14, 14.5, 15), BLUE)]


SMALL_BARS = [(1, 1), (0.5, 0.5), (1.5, 0.5), (0.5, 1), (1, 0.5), (0.5, 0.5), (1, 0)]


@icon('Barcode16', 16)
def _():
    return [shape(rect(1.5, 2.5, 14.5, 13.5), PAPER, P1)] + bars(3, 4.5, 10.5, SMALL_BARS) + [stroke('M4,12 H12', INK_SOFT, 1, dash='1,1')]


@icon('QRCode16', 16)
def _():
    def finder(x, y):
        return [shape(rect(x + 0.5, y + 0.5, x + 5.5, y + 5.5), PAPER, Pen(INK)), shape(rect(x + 2, y + 2, x + 4, y + 4), INK)]
    mods = [(8, 1), (8, 4), (1, 8), (4, 8), (8, 8), (11, 8), (11, 11), (8, 13), (13, 13), (13, 9)]
    return finder(1, 1) + finder(9, 1) + finder(1, 9) + [shape(rect(x, y, x + 2, y + 2), INK) for x, y in mods]


@icon('DataMatrix16', 16)
def _():
    top = [shape(rect(x, 1, x + 2, 3), INK) for x in (1, 5, 9, 13)]
    side = [shape(rect(13, y, 15, y + 2), INK) for y in (5, 9)]
    inner = [shape(rect(x, y, x + 2, y + 2), INK) for x, y in ((5, 5), (9, 7), (5, 9), (9, 11), (7, 7))]
    return [shape(rect(1, 1, 3, 15), INK), shape(rect(1, 13, 15, 15), INK)] + top + side + inner


@icon('Picture16', 16)
def _():
    return [shape(rect(1.5, 2.5, 14.5, 13.5), BLUE_L, Pen(BLUE)),
            shape(poly([(2.5, 12.5), (6.5, 7), (9.5, 10.5), (11.5, 8.5), (14, 12.5)]), GREEN_M),
            shape(circle(11, 5.5, 1.5), YELLOW)]


@icon('Rectangle16', 16)
def _():
    return [stroke(rect(2.5, 4.5, 13.5, 11.5), INK)]


@icon('Line16', 16)
def _():
    return [stroke('M3,13 L13,3', INK, 1.5, cap='Round')]


@icon('Ellipse16', 16)
def _():
    return [stroke(ellipse(8, 8, 6, 4.5), INK, 1.2)]


@icon('Inverse16', 16)
def _():
    return [shape(rect(2.5, 3.5, 8, 12.5), INK), shape(rect(8, 3.5, 13.5, 12.5), PAPER),
            stroke(rect(2.5, 3.5, 13.5, 12.5), INK),
            stroke('M4,6.5 H6.5 M4,9.5 H6.5', PAPER), stroke('M9.5,6.5 H12 M9.5,9.5 H12', INK)]

# ---------------------------------------------------------------- Guide topics and notes


@icon('Home16', 16)
def _():
    return [shape(rect(3.5, 7.5, 12.5, 14.5), BLUE_L, Pen(BLUE)), shape(rect(6.5, 10, 9.5, 14.5), BLUE),
            stroke('M1.5,8.5 L8,2 L14.5,8.5', BLUE, 1.4, cap='Round', join='Round')]


@icon('QuickStart16', 16)
def _():
    return [shape(poly([(9.5, 1), (3, 9), (7.2, 9), (6, 15), (13, 6.5), (8.8, 6.5)]), YELLOW, Pen(YELLOW_D, 1, join='Round'))]


@icon('Ribbon16', 16)
def _():
    return [shape(rect(1.5, 3.5, 14.5, 12.5), PAPER, P1), shape(rect(1.5, 3.5, 14.5, 5.5), BLUE, P1),
            shape(rect(3.5, 7.5, 6.5, 10.5), BLUE_L, Pen(BLUE)), shape(rect(9.5, 7.5, 12.5, 10.5), ORANGE_L, Pen(ORANGE))]


@icon('Canvas16', 16)
def _():
    return [shape(rect(1.5, 1.5, 14.5, 4.5), GRAY_L, Pen(GRAY_D)), shape(rect(1.5, 4.5, 4.5, 14.5), GRAY_L, Pen(GRAY_D)),
            shape(rect(6.5, 6.5, 13.5, 12.5), PAPER, Pen(BLUE)), stroke('M8,8.5 H12 M8,10.5 H10.5', BLUE_M)]


@icon('Properties16', 16)
def _():
    return [shape(rect(1.5, 1.5, 14.5, 14.5), PAPER, P1), stroke('M3.5,5.5 H12.5 M3.5,10.5 H12.5', BLUE_M),
            shape(rect(5.5, 3.5, 7.5, 7.5), BLUE), shape(rect(9.5, 8.5, 11.5, 12.5), BLUE)]


@icon('Calibration16', 16)
def _():
    return [stroke(circle(7.5, 7.5, 5.5), RED, 1.2), stroke(circle(7.5, 7.5, 2.2), RED, 1.2),
            stroke('M7.5,0.5 V3.5 M7.5,11.5 V14.5 M0.5,7.5 H3.5 M11.5,7.5 H14.5', INK)]


@icon('Keyboard16', 16)
def _():
    return [shape(rrect(0.5, 3.5, 15.5, 12.5, 1.5), GRAY_L, Pen(INK_SOFT)),
            stroke('M2.5,6.5 H4 M5.5,6.5 H7 M8.5,6.5 H10 M11.5,6.5 H13.5 M3.5,8.5 H5 M6.5,8.5 H8 M9.5,8.5 H11 M12.5,8.5 H13.5', INK_SOFT),
            stroke('M4.5,10.5 H11.5', INK_SOFT)]


@icon('Help16', 16)
def _():
    return [shape(circle(8, 8, 7), BLUE),
            stroke('M5.6,6 C5.6,2.8 10.4,2.8 10.4,5.8 C10.4,7.8 8,7.9 8,10', PAPER, 1.6, cap='Round'),
            shape(circle(8, 12.6, 1), PAPER)]


@icon('About16', 16)
def _():
    return [shape(circle(8, 8, 6.5), BLUE_L, Pen(BLUE)), shape(circle(8, 4.8, 1.1), BLUE), shape(rect(7, 6.8, 9, 12), BLUE)]


@icon('UserGuide16', 16)
def _():
    return [shape(poly([(1.5, 3), (7.5, 4), (7.5, 13.5), (1.5, 12.5)]), PAPER, Pen(BLUE)),
            shape(poly([(8.5, 4), (14.5, 3), (14.5, 12.5), (8.5, 13.5)]), PAPER, Pen(BLUE)),
            shape(rect(7.5, 4, 8.5, 13.5), BLUE),
            stroke('M3,6 L6,6.5 M3,8.5 L6,9 M10,6.5 L13,6 M10,9 L13,8.5', LINE)]


@icon('Tag16', 16)
def _():
    return [shape(poly([(8.5, 1.5), (14.5, 1.5), (14.5, 7.5), (7.5, 14.5), (1.5, 8.5)]), ORANGE_L, Pen(ORANGE, 1, join='Round')),
            shape(circle(11.5, 4.5, 1.3), PAPER, Pen(ORANGE))]


@icon('Templates16', 16)
def _():
    return [shape(rect(1.5, 1.5, 6.5, 6.5), BLUE_L, Pen(BLUE)), shape(rect(9.5, 1.5, 14.5, 6.5), BLUE_L, Pen(BLUE)),
            shape(rect(1.5, 9.5, 6.5, 14.5), BLUE_L, Pen(BLUE)), shape(rect(9.5, 9.5, 14.5, 14.5), PAPER, Pen(INK_SOFT))]


@icon('Tip16', 16)
def _():
    return [shape('M8,1.5 C11,1.5 13,3.6 13,6.3 C13,8.4 11.6,9.4 10.8,10.6 V11.5 H5.2 V10.6 C4.4,9.4 3,8.4 3,6.3 C3,3.6 5,1.5 8,1.5 Z',
                  YELLOW_L, Pen(YELLOW_D)),
            stroke('M5.5,13 H10.5 M6.5,14.8 H9.5', INK_SOFT, 1.2)]


@icon('Ok16', 16)
def _():
    return [shape(circle(8, 8, 6.5), GREEN_M, Pen(GREEN)),
            stroke('M4.8,8.2 L7,10.4 L11.2,5.8', PAPER, 1.6, cap='Round', join='Round')]


@icon('Warning16', 16)
def _():
    return [shape(poly([(8, 1.5), (15, 14), (1, 14)]), YELLOW, Pen(YELLOW_D, 1, join='Round')),
            shape(rect(7.2, 5.5, 8.8, 10), INK), shape(circle(8, 12, 0.9), INK)]


@icon('Error16', 16)
def _():
    return [shape(circle(8, 8, 6.5), RED, Pen(RED)),
            stroke('M5.4,5.4 L10.6,10.6 M10.6,5.4 L5.4,10.6', PAPER, 1.6, cap='Round')]


@icon('PictureSize32', 32)
def _():
    return [shape(rect(2.5, 6.5, 22.5, 25.5), BLUE_L, Pen(BLUE)),
            shape(poly([(4, 24), (10, 15), (14, 20), (17, 17), (21, 24)]), GREEN_M),
            shape(circle(17.5, 11, 2), YELLOW),
            stroke('M19.5,29 L29,19.5', ORANGE, 1.6, cap='Round'),
            shape(poly([(29.5, 19), (29.5, 24), (24.5, 19)]), ORANGE),
            shape(poly([(19, 29.5), (24, 29.5), (19, 24.5)]), ORANGE)]


@icon('OriginalSize16', 16)
def _():
    a, h = arc_arrow(11.5, 11.5, 3.2, 200, 470, 2)
    return [shape(rect(1.5, 1.5, 11.5, 10.5), BLUE_L, Pen(BLUE)),
            shape(poly([(2.5, 9.5), (5.5, 5.5), (7.5, 7.5), (8.5, 6.5), (10.5, 9.5)]), GREEN_M),
            shape(circle(11.5, 11.5, 4), PAPER),
            stroke(a, ORANGE, 1.3), shape(h, ORANGE)]


def emit():
    out = ['<!-- ANLAbel ribbon icons (docs/NICELABEL_10_UI_LAYOUT.md, "Icons"): large icons 32 x 32 DIP,',
           '     small 16 x 16 DIP, one flat palette, 1 DIP outlines on half-pixel coordinates. Each icon',
           '     starts with a transparent square so it measures exactly its size. ANLAbel artwork only. -->',
           '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
           '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
           '                    xmlns:po="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"',
           '                    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"',
           '                    mc:Ignorable="po">']
    for name, (size, fn) in ICONS.items():
        parts = fn()
        frame = f'<GeometryDrawing Brush="#00FFFFFF" Geometry="M0,0 H{size} V{size} H0 Z"/>'
        out.append(f'    <DrawingImage x:Key="Icon.{name}" po:Freeze="True"><DrawingImage.Drawing><DrawingGroup>')
        out.append('        ' + frame)
        for p in parts:
            out.append('        ' + p)
        out.append('    </DrawingGroup></DrawingImage.Drawing></DrawingImage>')
    out.append('</ResourceDictionary>')
    return '\r\n'.join(out) + '\r\n'


if __name__ == '__main__':
    text = emit()
    with open(OUT, 'w', encoding='utf-8', newline='') as fh:
        fh.write(text)
    print(len(ICONS), 'icons ->', OUT)
