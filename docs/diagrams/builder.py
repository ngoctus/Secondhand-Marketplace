import xml.etree.ElementTree as ET
import html
import os

class DrawioDiagramBuilder:
    def __init__(self):
        self.mxfile = ET.Element('mxfile', {
            'host': 'app.diagrams.net',
            'agent': 'Antigravity-DFD-Generator',
            'version': '24.7.17'
        })

    def add_page(self, page_id, page_name, elements, connectors):
        """
        elements: list of dicts:
            {'id': 'e1', 'type': 'entity'|'process'|'store'|'title'|'group', 
             'label': 'Text', 'x': 100, 'y': 100, 'w': 140, 'h': 60}
        connectors: list of dicts:
            {'id': 'c1', 'source': 'e1', 'target': 'p1', 'label': 'Label text',
             'exitX': 1.0, 'exitY': 0.5, 'entryX': 0.0, 'entryY': 0.5, 'waypoints': [(x,y)]}
        """
        diagram = ET.SubElement(self.mxfile, 'diagram', {
            'id': page_id,
            'name': page_name
        })
        
        graph_model = ET.SubElement(diagram, 'mxGraphModel', {
            'dx': '1422',
            'dy': '850',
            'grid': '1',
            'gridSize': '10',
            'guides': '1',
            'tooltips': '1',
            'connect': '1',
            'arrows': '1',
            'fold': '1',
            'page': '1',
            'pageScale': '1',
            'pageWidth': '1400',
            'pageHeight': '900',
            'math': '0',
            'shadow': '0'
        })
        
        root = ET.SubElement(graph_model, 'root')
        ET.SubElement(root, 'mxCell', {'id': '0'})
        ET.SubElement(root, 'mxCell', {'id': '1', 'parent': '0'})
        
        # Styles
        styles = {
            'entity': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#e1d5e7;strokeColor=#9673a6;fontColor=#333333;fontStyle=1;fontSize=12;fontFamily=Helvetica;strokeWidth=1.5;shadow=1;',
            'process': 'rounded=1;arcSize=28;whiteSpace=wrap;html=1;fillColor=#dae8fc;strokeColor=#6c8ebf;fontColor=#003366;fontStyle=1;fontSize=11;fontFamily=Helvetica;strokeWidth=1.5;shadow=1;spacing=4;',
            'store': 'shape=partialRectangle;top=0;bottom=0;fillColor=#fff2cc;strokeColor=#d6b656;fontColor=#663300;fontStyle=1;fontSize=11;fontFamily=Helvetica;strokeWidth=1.5;align=center;verticalAlign=middle;whiteSpace=wrap;html=1;spacing=2;',
            'title': 'text;html=1;strokeColor=none;fillColor=#f5f5f5;align=left;verticalAlign=middle;rounded=1;fontStyle=1;fontSize=14;fontColor=#333333;strokeColor=#cccccc;spacingLeft=15;',
            'note': 'shape=note;whiteSpace=wrap;html=1;backgroundOutline=1;darkOpacity=0.05;fillColor=#ffffc0;strokeColor=#ffcc00;fontColor=#666600;fontSize=10;fontFamily=Helvetica;align=left;spacing=8;'
        }
        
        # Add elements
        for el in elements:
            el_type = el.get('type', 'process')
            style = el.get('custom_style', styles.get(el_type, styles['process']))
            cell = ET.SubElement(root, 'mxCell', {
                'id': el['id'],
                'value': el['label'],
                'style': style,
                'vertex': '1',
                'parent': '1'
            })
            ET.SubElement(cell, 'mxGeometry', {
                'x': str(el['x']),
                'y': str(el['y']),
                'width': str(el['w']),
                'height': str(el['h']),
                'as': 'geometry'
            })
            
        # Add connectors
        for c in connectors:
            c_style = (
                "edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;"
                "strokeColor=#4a5568;strokeWidth=1.5;fontSize=10;fontFamily=Helvetica;fontColor=#2d3748;"
                "labelBackgroundColor=#ffffff;spacing=3;endArrow=classic;endFill=1;"
            )
            if 'exitX' in c and 'exitY' in c:
                c_style += f"exitX={c['exitX']};exitY={c['exitY']};exitDx=0;exitDy=0;"
            if 'entryX' in c and 'entryY' in c:
                c_style += f"entryX={c['entryX']};entryY={c['entryY']};entryDx=0;entryDy=0;"
                
            cell = ET.SubElement(root, 'mxCell', {
                'id': c['id'],
                'value': c['label'],
                'style': c_style,
                'edge': '1',
                'parent': '1',
                'source': c['source'],
                'target': c['target']
            })
            geo = ET.SubElement(cell, 'mxGeometry', {
                'relative': '1',
                'as': 'geometry'
            })
            if 'waypoints' in c and c['waypoints']:
                arr = ET.SubElement(geo, 'Array', {'as': 'points'})
                for wx, wy in c['waypoints']:
                    ET.SubElement(arr, 'mxPoint', {'x': str(wx), 'y': str(wy)})

    def save(self, output_path):
        tree = ET.ElementTree(self.mxfile)
        ET.indent(tree, space="  ", level=0)
        tree.write(output_path, encoding='utf-8', xml_declaration=True)
        print(f"Diagram saved to {output_path}")

print("Builder class defined successfully.")
