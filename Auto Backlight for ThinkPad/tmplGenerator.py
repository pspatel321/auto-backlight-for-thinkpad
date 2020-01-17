desc = '''
Convert input file (template .cs) into generated code (.g.cs)
'''

if __name__ == "__main__":
    import os, sys, argparse, re, xml.etree.ElementTree as ET

    parser = argparse.ArgumentParser(description=desc)
    parser.add_argument('--out', nargs=1, help='explicit output file path')
    parser.add_argument('file', nargs=1, help='input file path')
    args = parser.parse_args()

    # Output file name
    if args.out is None:
        d, f = os.path.split(args.file[0])
        f = f.replace(".tmpl.cs", ".cs") if f.endswith(".tmpl.cs") else f.replace(".cs", ".g.cs") if f.endswith(".cs") else f + ".cs"
        args.out = [os.path.join(d, f)]

    # Pre-define bunch of re expressions
    tagName = 'tmpl'
    # Get contents of line comments
    re_lineComm = re.compile(r'^[\t ]*\/{2,}[\t ]*(.*)(?<![\t ])', flags=re.M)
    # Get contents of block comments
    re_blockComm = re.compile(r'\/\*([\s\S]*?)\*\/', flags=re.M)
    # Get xml tag start from a parsed comment
    re_tagStart = re.compile(r'(?<!\S)(<' + tagName + r'(?=[\s>]).*?>)(?!\S)')
    # Get xml tag end from a parsed comment
    re_tagEnd = re.compile(r'(?<!\S)(<\/' + tagName + r'>)(?!\S)')

    # Locate and extract the next xml tagged block
    def extractOneBlock(string:str) -> (str,str,str,dict):
        # Which occurs first, line or block style?
        lst = None
        for comm in re_lineComm.finditer(string):
            lst = re_tagStart.search(comm.group(1))
            if lst:
                lst = comm
                break
        bst = None
        for comm in re_blockComm.finditer(string):
            bst = re_tagStart.search(comm.group(1))
            if bst:
                bst = comm
                break
        endComm = None
        if lst and not bst:
            endComm = re_lineComm
        if bst and not lst:
            endComm = re_blockComm
        if lst and bst:
            if lst.start() < bst.start():
                endComm = re_lineComm
            if lst.start() > bst.start():
                endComm = re_blockComm
        if endComm is None:
            return None

        # Get start tag and end tag of the chosen comment style
        chunkPre = None
        chunkPost = None
        for comm in endComm.finditer(string):
            cs = re_tagStart.search(comm.group(1))
            ce = re_tagEnd.search(comm.group(1))
            if not chunkPre and cs:
                chunkPre = comm.span()
                tagPre = cs.group(1)
            if chunkPre and ce:
                chunkPost = comm.span()
                tagPost = ce.group(1)

                # Package data and return
                xml = tagPre + tagPost
                xml = ET.fromstring(xml.encode()).attrib
                if xml is None:
                    xml = dict()
                pre = string[0:chunkPre[0]]
                body = string[chunkPre[1]+1:chunkPost[0]]
                post = string[chunkPost[1]+1:]
                return pre, body, post, xml
        return None

    # Process keys and header string into organized structures
    def loadKeys(hdr:str, string:str) -> (tuple,tuple):
        keys = list()
        header = [h.strip('\t ') for h in hdr.split(';')]
        for line in string.splitlines():
            line = re.search(r'([\w\"].*)', line).group(1)

            # Remove quoted strings, so their insides stay intact
            qloc = [qs.start() for qs in re.finditer(r'(?<!\\)\"', line)]
            if (len(qloc) % 2) != 0:
                raise Exception("Unmatched quotes on quoted-string")
            qs = list()
            for i in range(0, len(qloc), 2):
                s = line[qloc[i]:qloc[i+1]+1]
                line = line[0:qloc[i]] + '\n' + line[qloc[i+1]+1:]
                qloc = [q - len(s) + 1 for q in qloc]
                qs.append(s)
                
            # Separate by semi-colons, replace back quoted strings if needed
            words = tuple(qs.pop(0) if w.strip('\t ') is '\n' else w.strip('\t ') for w in line.split(';'))
            if len(header) != len(words):
                raise Exception("Incorrect number of words detected compared to Keys header")                
            keys.append(words)
        return tuple(header), tuple(keys)

    # Perform text substitutions using entries in keys and header
    def performSubstitutions(header:tuple, keys:tuple, string:str) -> str:
        if keys is None or header is None:
            raise Exception("Keys were not found")
        out = ''
        i = 0
        for k in keys:
            s = string
            j = 0
            for h in header:
                s = re.sub(r'(?<![A-Za-z0-9])' + h + r'(?![A-Za-z0-9])', k[j], s)
                j += 1
            s = re.sub(r'(?<![A-Za-z0-9])' + 'i_' + r'(?![A-Za-z0-9])', f'{i}', s)
            out += s
            i += 1
        return out

    inpStr = ''
    with open(args.file[0], 'r', encoding='utf-8-sig') as file:
        inpStr = file.read()

    # Split file into chunks, each annotated with the action (None = pure text, xml attribs = tmpl section)
    splitStrs = [(None, inpStr)]
    while True:
        ret = extractOneBlock(splitStrs[-1][1])
        if ret is None:
            break

        splitStrs.pop()
        splitStrs.append((None, ret[0]))
        splitStrs.append((ret[3], ret[1]))
        splitStrs.append((None, ret[2]))

    outputStr = ''
    header = None
    keys = None

    # Re-assemble the chunks, performing processing along the way
    for act, string in splitStrs:
        if act is None:
            outputStr += string
            continue
        if 'Keys' in act.keys():
            header, keys = loadKeys(act.get('Keys'), string)
            continue
        if 'Ignore' in act.keys():
            if act.get('Ignore') == "true":
                continue
        if keys is None:
            continue
        
        out = performSubstitutions(header, keys, string)
        outputStr += out

    # Save file
    with open(args.out[0], 'w', encoding='utf-8-sig') as file:
        file.write(outputStr)
