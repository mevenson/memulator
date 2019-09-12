*
        TTL     THEBUG
        OPT     PAG
        PAG
*
* DEBUG VARIABLE STORAGE
*
IOADDR  EQU     $8000
CTLPORT EQU     IOADDR+$4
PIA     EQU     IOADDR+28
*
RAM     EQU     $A000
PROM    EQU     $F000
ROM     EQU     $E000
*
COMREG  EQU     IOADDR+24
DATAREG EQU     IOADDR+27
SECREG  EQU     IOADDR+26
DRVREG  EQU     IOADDR+20
PROMPT  EQU     ':
*
        ORG     RAM
*
IRQ     RMB     2
BEGA    RMB     2
ENDA    RMB     2
NMI     RMB     2
SP      RMB     2
PORTADR RMB     2
ECHFLG  RMB     1
INXMSB  RMB     1
INXLSB  RMB     1
CHKSUM  RMB     1
TEMP16  RMB     2
SWI     RMB     2
CRC16   RMB     2
XTEMP   RMB     2
BREAKPT RMB     15
BREAKND EQU     *
PRNTFLG RMB     1
PFLAG   RMB     1
*
BYTECT  EQU     RAM+$47
GOADDR  EQU     RAM+$48
*
STKORG  EQU     RAM+$C0
*
        ORG     RAM+96
*
TSTACK  RMB     2
OPSAVE  RMB     1
PB1     RMB     1
PB2     RMB     1
PB3     RMB     1
PC1     RMB     1
PC2     RMB     1
BPOINT  RMB     4
BKFLG   RMB     1
BKFLG2  RMB     1
MCONT   RMB     1
TFLAG   RMB     1
BFLAG   RMB     1
MFLAG   RMB     1
XFLAG   RMB     1
BITE2   RMB     1
BITE3   RMB     1
XTEMP1  RMB     2
CRC0    RMB     2
TEMP    RMB     2
*
LDADDR  EQU     $A100
LOADCNT EQU     $0100
*
*
        PAG
*
        ORG     ROM
*
IRQV    LDX     IRQ
        JMP     0,X
SWIV    LDX     SWI
        JMP     0,X
NMIV    LDX     NMI
        JMP     0,X
*
PATCH   STA A   STKORG+1
        LDX     #RTIINS
        STX     NMI
        STX     IRQ
        CLR     TFLAG
        CLR     BKFLG
        CLR     BKFLG2
        CLR     PRNTFLG
        JSR     PINIT
        RTS
*
        ORG     ROM+$3F
LDABRT  LDA A   #$3F
        BSR     OUTCH
        RTS
*
        ORG     ROM+$47
BADDR   BSR     BYTE
        STA A   INXMSB
        BSR     BYTE
        STA A   INXLSB
        LDX     INXMSB
        RTS
*
BYTE    BSR     INHEX
BYTE1   ASL A
        ASL A
        ASL A
        ASL A
        TAB
        BSR     INHEX
        ABA
        TAB
        ADD B   CHKSUM
        STA B   CHKSUM
        RTS
*
*
OUTHL   LSR A
        LSR A
        LSR A
        LSR A
OUTHR   AND A   #$0F
        ADD A   #$30
        CMP A   #$39
        BLS     OUTCH
        ADD A   #$07
*
OUTCH   JMP     OUTEEE
INCH    JMP     INEEE
*
PDATA2  BSR     OUTCH
        INX
PDATA1  LDA A   0,X
        CMP A   #4
        BNE     PDATA2
        RTS
*
        ORG     ROM+$8C
ACIASET LDA B   #$15
ACIAS1  LDA A   #$03
        STA A   0,X
        STA B   0,X
        RTS
*
GO      LDA A   GOADDR
        LDA B   GOADDR+1
        LDS     SP
        TSX
        STA A   5,X
        STA B   6,X
RTIINS  RTI
*
JUMP    INS
        INS
CALL    BSR     BADDR
        JMP     0,X
*
INHEX   BSR     INCH
INHEX1  SUB A   #$30
        BMI     ABORT
        CMP A   #9
        BLE     INHRET
        CMP A   #$11
        BMI     ABORT
        CMP A   #$16
        BGT     ABORT
        SUB A   #7
INHRET  RTS
*
OUT2H   LDA A   0,X
        BSR     OUTHL
        LDA A   0,X
        INX
        BRA     OUTHR
*
OUT4HS  BSR     OUT2H
OUT2HS  BSR     OUT2H
OUTS    LDA A   #$20
        BRA     OUTCH
*
START   LDS     #STKORG
        LDA A   #$FF
        JSR     PATCH
        JSR     SWICLR
        JSR     SWISET
        LDX     #CTLPORT
        BSR     ACIASET
*
MONITOR LDS     #STKORG
        STS     SP
DEBUG   LDX     #CTLPORT
        STX     PORTADR
ABORT   LDS     SP
        CLR     ECHFLG
        BSR     PCRLFP
GETCMD  JSR     INEEE
*
* LOOK UP COMMAND IN COMMAND TABLE
*
        LDX     #CMDTBL
AGAIN   CMP A   0,X
        BEQ     FNDCMD
        CPX     #TBLEND
        BEQ     ABORT
*
        INX
        INX
        INX
        BRA     AGAIN
*
FNDCMD  BSR     OUTS
        LDX     1,X
        JSR     0,X
        BRA     ABORT
*
SWIENT  STS     SP
        TSX
        JSR     SWISCL
        BSR     REGS
        BRA     ABORT
*
REGS    BSR     PCRLFP
        LDX     SP
        INX
        BSR     OUT2HS
        BSR     OUT2HS
        BSR     OUT2HS
        BSR     OUT4HS
        BSR     OUT4HS
        LDX     #SP
        BRA     OUT4HS
*
PCRLFP  LDX     #MCLOFF
        JMP     PDATA1
*
CHANGE  JSR     BADDR
MEMLOC  JSR     PCRLF
        LDX     #INXMSB
        BSR     OUT4HS
        LDX     INXMSB
        BSR     OUT2HS
        DEX
EATLSP  BSR     INEEE
        BSR     ALTER
        BCC     EATLSP
        CMP A   #$20
        BEQ     NEXADX
        CMP A   #PROMPT
        BEQ     NOTHEX
        CMP A   #$0D
        BEQ     NOTHEX
        CMP A   #$5E
        BNE     NEXADR
        DEX
        DEX
NEXADR  INX
NEXADX  STX     INXMSB
        BRA     MEMLOC
ALTER   CMP A   #$30
        BCS     NOTHEX
        CMP A   #$3A
        BCS     ALTER1
        CMP A   #$41
        BCS     NOTHEX
        CMP A   #$47
        BCS     ALTER1
NOTHEX  SEC
        RTS
ALTER1  JSR     INHEX1
        JSR     BYTE1
        STA A   0,X
        CMP A   0,X
        BEQ     ALTER2
        JMP     LDABRT
ALTER2  INX
        JSR     OUTS1
        CLC
        RTS
*
        ORG     ROM+$19C
*
MCLOFF  FCB     $13
MCL     FCB     $D,$A,$15,0,0,0
        FCB     PROMPT,4
*
SAVEGET STX     XTEMP
        LDX     PORTADR
        RTS
*
INEEE   BSR     INCH8
        AND A   #$7F
        RTS
*
INCH8   PSH B
        BSR     SAVEGET
ACIAIN  LDA A   0,X
        ASR A
        BCC     ACIAIN
        LDA A   1,X
        LDA B   ECHFLG
        BNE     RET
ACIAOUT LDA B   0,X
        ASR B
        ASR B
        BCC     ACIAOUT
        STA A   1,X
RET     PUL B
        LDX     XTEMP
        RTS
*
        ORG     ROM+$1D1
*
OUTEEE  PSHB
        BSR     SAVEGET
        TST     PRNTFLG
        BEQ     ACIAOUT
*
POUT    BSR     PCHK
        BPL     POUT
        CLR     PFLAG
        STA A   PIA
        LDA B   #$36
        STA B   PIA+1
        LDA B   #$3E
        STA B   PIA+1
        BRA     ACIAOUT
*
PCHK    TST     PFLAG
        BMI     PEXIT
        TST     PIA+1
        BPL     PEXIT
PREADY  TST     PIA
        COM     PFLAG
PEXIT   RTS
*
PINIT   LDA A   #$3A
        STA A   PIA+1
        LDA A   #$FF
        STA A   PIA
        LDA A   #$3E
        STA A   PIA+1
        RTS
*
GETPAIR JSR     BADDR
        BSR     OUTS1
        STX     BEGA
        JSR     BADDR
        INX
        STX     ENDA
OUTS1   JMP     OUTS
*
TESTMD  BSR     GETPAIR
        CLR     TEMP16
TESTD1  BSR     TESTMR
TESTD2  BSR     TESTMX
        ADD B   TEMP16
        STA B   0,X
        BSR     INCADR
        BNE     TESTD2
        BSR     TESTMR
TESTD3  BSR     TESTMX
        ADD B   TEMP16
        EOR B   0,X
        BEQ     TESTD4
        STA B   CHKSUM
        BSR     TESTADR
        JSR     OUT2HS
        JSR     OUT2HS
TESTD4  BSR     INCADR
        BNE     TESTD3
        JSR     OUTPLUS
        INC     TEMP16
        BNE     TESTD1
        RTS
TESTADR JSR     PCRLF
        LDX     #INXMSB
        JMP     OUT4HS
*
PCRLF   LDX     #CRLF
        JMP     PDATA1
*
TESTMC  CLR     CRC16
        CLR     CRC16+1
TESTMR  LDX     BEGA
        STX     INXMSB
TESTMO  RTS
*
TESTMX  LDA B   INXMSB
        ADD B   INXLSB
        LDX     INXMSB
        LDA A   0,X
        RTS
*
INCADR  LDX     INXMSB
        INX
        STX     INXMSB
        CPX     ENDA
        RTS
*
OUTPLUS LDA A   #'+
        JMP     OUTEEE
*
BREAK   JSR     BADDR
        LDA A   0,X
        CMP A   #$3F
        BEQ     NOBREAK
        STA A   CHKSUM
        CLR A
        CLR B
        BSR     SWISRCH
        BCS     NOBREAK
        LDA A   INXMSB
        LDA B   INXLSB
        STA A   0,X
        STA B   1,X
        LDA A   CHKSUM
        STA A   2,X
        LDX     0,X
        LDA A   #$3F
        STA A   0,X
        RTS
*
NOBREAK JMP     LDABRT
*
SWICLR  LDX     #BREAKPT
SWINXT  STX     TEMP16
        BSR     CLRSWI
        BSR     SWINXA
        BNE     SWINXT
        RTS
SWISCL  LDX     5,X
        STX     GOADDR
        DEX
        STX     INXMSB
        LDA A   INXMSB
        LDA B   INXLSB
        BSR     SWISRCH
        BCS     SSRCH3
        STX     TEMP16
        TSX
        STA A   7,X
        STA B   8,X
        STA A   GOADDR
        STA B   GOADDR+1
        LDX     TEMP16
CLRSWI  LDA B   2,X
        LDX     0,X
        LDA A   0,X
        CMP A   #$3F
        BNE     CLRED
        STA B   0,X
        LDX     TEMP16
        CLR     0,X
        CLR     1,X
CLRED   RTS
*
SWISRCH LDX     #BREAKPT
SSRCH1  STX     TEMP16
        CMP A   0,X
        BNE     SSRCH2
        CMP B   1,X
        BEQ     SSRCH3
SSRCH2  BSR     SWINXA
        BNE     SSRCH1
        SEC
SSRCH3  RTS
*
SWINXA  LDX     TEMP16
        INX
        INX
        INX
        CPX     #BREAKND
        RTS
*
SWISET  LDX     #SWIENT
        STX     SWI
        LDX     #BREAKPT
SWISE1  CLR     0,X
        INX
        CPX     #BREAKND
        BNE     SWISE1
        RTS
*
BOOT    CLR A
        STA A   DRVREG
        LDX     #$FFFF
BOOT0   INX
        DEX
        DEX
        BNE     BOOT0
        LDA B   #$0B
        STA B   COMREG
        BSR     RETURN
BLOOP1  LDA B   COMREG
        BIT B   #1
        BNE     BLOOP1
        CLR     SECREG
        BSR     RETURN
        LDA B   #$9C
        STA B   COMREG
        BSR     RETURN
        LDX     #LDADDR
BLOOP2  BIT B   #2
        BEQ     BLOOP3
        LDA A   DATAREG
        STA A   0,X
        INX
BLOOP3  LDA B   COMREG
        BIT B   #1
        BNE     BLOOP2
        JMP     LDADDR
RETURN  RTS
*
* COMMAND TABLE
*
CMDTBL  FCB     'A
        FDB     ASCII
        FCB     'B
        FDB     BREAK
        FCB     'C
        FDB     CALL
        FCB     'D
        FDB     BOOT
        FCB     'E
        FDB     CHANGE
        FCB     'F
        FDB     FILL
        FCB     'G
        FDB     GO
        FCB     'H
        FDB     LDABRT
        FCB     'I
        FDB     DISSA
        FCB     'J
        FDB     JUMP
        FCB     'K
        FDB     SWICLR
        FCB     'L
        FDB     LOCATE
        FCB     'M
        FDB     MOVE
        FCB     'N
        FDB     LDABRT
        FCB     'O
        FDB     LDABRT
        FCB     'P
        FDB     LDABRT
        FCB     'Q
        FDB     TESTMD
        FCB     'R
        FDB     REGS
        FCB     'S
        FDB     LDABRT
        FCB     'T
        FDB     TRACE
        FCB     'U
        FDB     LDABRT
        FCB     'V
        FDB     VIEW
        FCB     'W
        FDB     LDABRT
        FCB     'X
        FDB     LDABRT
        FCB     'Y
        FDB     LDABRT
        FCB     $10
        FDB     HCOPY
        FCB     'Z
        FDB     LDABRT
        FCB     'a
        FDB     ASCII
        FCB     'b
        FDB     BREAK
        FCB     'c
        FDB     CALL
        FCB     'd
        FDB     BOOT
        FCB     'e
        FDB     CHANGE
        FCB     'f
        FDB     FILL
        FCB     'g
        FDB     GO
        FCB     'h
        FDB     LDABRT
        FCB     'i
        FDB     DISSA
        FCB     'j
        FDB     JUMP
        FCB     'k
        FDB     SWICLR
        FCB     'l
        FDB     LOCATE
        FCB     'm
        FDB     MOVE
        FCB     'n
        FDB     LDABRT
        FCB     'o
        FDB     LDABRT
        FCB     'p
        FDB     LDABRT
        FCB     'q
        FDB     TESTMD
        FCB     'r
        FDB     REGS
        FCB     's
        FDB     LDABRT
        FCB     't
        FDB     TRACE
        FCB     'u
        FDB     LDABRT
        FCB     'v
        FDB     VIEW
        FCB     'w
        FDB     LDABRT
        FCB     'x
        FDB     LDABRT
        FCB     'y
        FDB     LDABRT
TBLEND  FCB     'z
        FDB     LDABRT
*
HCOPY   LDA A   #$FF
        STA A   PFLAG
        COM     PRNTFLG
        INS
        INS
        JMP     GETCMD
*
MOVE    JSR     LIMITS
        LDX     #TOADD
        JSR     BAD1
        LDX     BEGA
        DEX
MOVE1   INX
        LDA A   0,X
        STX     BEGA
        LDX     INXMSB
        STA A   0,X
        INX
        STX     INXMSB
        LDX     BEGA
        CPX     ENDA
        BNE     MOVE1
        RTS
*
VIEW    JSR     BAD2
V1      LDA A   #8
        STA A   MCONT
V5      JSR     PCRLF
        JSR     OUTXHI
        LDA B   #16
V9      JSR     OUT2HS
        DEC B
        BIT B   #3
        BNE     V10
        JSR     OUTS
        CMP B   #0
V10     BNE     V9
        JSR     PCRLF
        LDA B   #5
        JSR     SKIP
        LDX     INXMSB
        LDA B   #16
V2      LDA A   0,X
        CMP A   #$20
        BCS     V3
        CMP A   #$5F
        BCS     V4
V3      LDA A   #'.
V4      JSR     OUTA
        INX
        DEC     B
        BNE     V2
        STX     INXMSB
        DEC     MCONT
        BNE     V5
        JSR     INEEE
        CMP A   #$20
        BEQ     V1
        CMP A   #'V
        BEQ     VIEW
        RTS
*
SKIP    LDA A   #$20
        JSR     OUTEEE
        DEC B
        BNE     SKIP
        RTS
*
OUTXHI  LDX     #INXMSB
        JSR     OUT4HS
        LDX     INXMSB
        RTS
*
PNTBYT  STA A   BYTECT
        LDX     #BYTECT
        JMP     OUT2H
*
SFE     STS     SP
        TSX
        TST     6,X
        BNE     DECP
        DEC     5,X
DECP    DEC     6,X
        LDS     #TSTACK
        TST     TFLAG
        BEQ     PRINT
        LDX     PC1
        LDA A   OPSAVE
        STA A   0,X
        TST     BFLAG
        BEQ     DISPLY
        LDX     BPOINT
        LDA A   BPOINT+2
        STA A   0,X
DISPLY  JMP     RETRN
*
BAD2    LDX     #FROMAD
        BRA     BAD1
ENDADD  LDX     #THRUAD
BAD1    JSR     PDATA1
        JMP     BADDR
LIMITS  BSR     BAD2
        STX     BEGA
        BSR     DOLF
        BSR     ENDADD
        STX     ENDA
DOLF    JMP     PCRLF
ADDR    LDX     #ADASC
        BRA     BAD1
VALUE   LDX     #VALASC
        JSR     PDATA1
        JMP     BYTE
*
CSET    FCC     'HINZVC'
*
S9      FCC     'S9'
        FCB     4
*
PRINT   LDX     SP
        LDA A   #6
        STA A   MCONT
        LDA B   1,X
        ASL B
        ASL B
        LDX     #CSET
DSOOP   LDA A   #'-
        ASL B
        BCC     DSOOP1
        LDA A   0,X
DSOOP1  JSR     OUTEEE
        INX
        DEC     MCONT
        BNE     DSOOP
        LDX     #BREG
        JSR     PDATA1
        LDX     SP
        INX
        INX
        JSR     OUT2HS
        STX     TEMP
        LDX     #AREG
        JSR     PDATA1
        LDX     TEMP
        JSR     OUT2HS
        STX     TEMP
        LDX     #XREG
        JSR     PDATA1
        LDX     TEMP
        JSR     OUT4HS
        STX     TEMP
        TST     TFLAG
        BNE     PNTS
        LDX     #PCTR
        JSR     PDATA1
        LDX     TEMP
        JSR     OUT4HS
PNTS    LDX     #SREG
        JSR     PDATA1
        LDX     #SP
        TST     TFLAG
        BNE     PRINTS
        JSR     OUT4HS
        LDA A   BKFLG
        BEQ     C2
        LDX     PB2
        STA A   0,X
        LDX     BKFLG2
        BEQ     C2
        LDX     PC1
        STA A   0,X
C2      JMP     MONITOR
PRINTS  LDA B   0,X
        LDA A   1,X
        ADD A   #7
        ADC B   #0
        STA B   TEMP
        STA A   TEMP+1
        LDX     #TEMP
        JMP     OUT4HS
*
DISSA   JSR     BAD2
        CLR     TFLAG
        BRA     DISS
*
TRACE   LDX     #SFE
        STX     SWI
        JSR     BAD2
        JSR     PCRLF
        LDX     SP
        LDA B   INXMSB
        STA B   6,X
        LDA A   INXLSB
        STA A   7,X
KONTIN  INC     TFLAG
RETRN   JSR     PRINT
        LDX     SP
        LDX     6,X
DISS    STX     PC1
DISIN   JSR     PCRLF
        LDX     #PC1
        JSR     OUT4HS
        LDX     #BFLAG
        LDA A   #5
CLEAR   CLR     0,X
        INX
        DEC A
        BNE     CLEAR
        LDX     PC1
        LDA B   0,X
        JSR     OUT2HS
        STX     PC1
        LDA A   0,X
        STA A   PB2
        LDA A   1,X
        STA A   PB3
        STA B   PB1
        TBA
        JSR     TBLKUP
        LDA A   TEMP
        CMP A   #'*'
        BNE     OKOP
        JMP     NOTBB
OKOP    LDA A   PB1
        CMP A   #$8D
        BNE     NEXT
        INC     BFLAG
        BRA     PUT1
NEXT    AND A   #$F0
        CMP A   #$60
        BEQ     ISX
        CMP A   #$A0
        BEQ     ISX
        CMP A   #$E0
        BEQ     ISX
        CMP A   #$80
        BEQ     IMM
        CMP A   #$C0
        BNE     PUT1
IMM     INC     MFLAG
        LDX     #SPLBOO
        BRA     PUT
ISX     INC     XFLAG
        LDA A   PB2
        JSR     PNTBYT
        LDX     #COMMX
PUT     JSR     PDATA1
PUT1    LDX     PC1
        LDA A   PB1
        CMP A   #$8C
        BEQ     BYT3
        CMP A   #$8E
        BEQ     BYT3
        CMP A   #$CE
        BEQ     BYT3
        AND A   #$F0
        CMP A   #$20
        BNE     NOTB
        INC     BFLAG
        BRA     BYT2
NOTB    CMP A   #$60
        BCS     BYT1
        AND A   #$30
        CMP A   #$30
        BNE     BYT2
BYT3    INC     BITE3
        TST     MFLAG
        BNE     BYT31
        LDA A   #'$'
        JSR     OUTEEE
BYT31   LDA A   0,X
        INX
        STX     PC1
        JSR     PNTBYT
        LDX     PC1
        BRA     BYT21
BYT2    INC     BITE2
BYT21   LDA A   0,X
        INX
        STX     PC1
        TST     XFLAG
        BNE     BYT1
        TST     BITE3
        BNE     BYT22
        TST     MFLAG
        BNE     BYT22
        TAB
        LDA A   #'$'
        JSR     OUTEEE
        TBA
BYT22   JSR     PNTBYT
BYT1    TST     BFLAG
        BEQ     NOTBB
        LDA B   #3
        JSR     SKIP
        CLR A
        LDA B   PB2
        BGE     DPOS
        LDA A   #$FF
DPOS    ADD B   PC2
        ADC A   PC1
        STA A   BPOINT
        STA B   BPOINT+1
        LDX     #BPOINT
        JSR     OUT4HS
NOTBB   LDA B   #$0D
        LDA A   #1
        TST     BITE2
        BEQ     NOTBB3
        LDA B   #1
        TST     BFLAG
        BNE     NOTBB2
        LDA B   #8
        TST     MFLAG
        BNE     NOTBB2
        TST     XFLAG
        BNE     NOTBB2
        LDA B   #9
NOTBB2  LDA A   #2
        BRA     NOTBB8
NOTBB3  TST     BITE3
        BEQ     NOTBB8
        LDA A   #3
        LDA B   #6
        TST     MFLAG
        BEQ     NOTBB8
        LDA B   #5
NOTBB8  PSH A
        JSR     SKIP
        PUL B
        LDX     #PB1
NOTBB4  LDA A   0,X
        CMP A   #$20
        BLE     NOTBB5
        CMP A   #$60
        BLE     NOTBB9
NOTBB5  LDA A   #'.'
NOTBB9  INX
        JSR     OUTEEE
        DEC B
        BNE     NOTBB4
NOT1    JSR     INEEE
        TAB
        JSR     OUTS
        CMP B   #$20
        BEQ     DOT
CHCBA   LDX     SP
        INX
        CMP B   #'C'
        BEQ     RDC
        INX
        CMP B   #'B'
        BEQ     RDC
        INX
        CMP B   #'A'
        BEQ     RDC
        INX
        CMP B   #'X'
        BEQ     RDX
        LDX     #SP
        CMP B   #'S'
        BNE     RETNOT
RDX     JSR     BYTE
        STA A   0,X
        INX
RDC     JSR     BYTE
        STA A   0,X
        JSR     PCRLF
        JSR     PRINT
        BRA     NOT1
RETNOT  JSR     PCRLF
        LDS     #STKORG
        LDA A   #$FF
        JSR     PATCH
        JSR     SWICLR
        JSR     SWISET
        JMP     MONITOR
DOT     TST     TFLAG
        BNE     DOT1
        JMP     DISIN
*
DOT1    LDA B   #$3F
        LDA A   PB1
        CMP A   #$8D
        BNE     TSTB
        LDX     BPOINT
        STX     PC1
        CLR     BFLAG
TSTB    TST     BFLAG
        BEQ     TSTJ
        LDX     BPOINT
        LDA A   0,X
        STA A   BPOINT+2
        STA B   0,X
        BRA     EXEC
TSTJ    CMP A   #$6E
        BEQ     ISXD
        CMP A   #$AD
        BEQ     ISXD
        CMP A   #$7E
        BEQ     ISJ
        CMP A   #$BD
        BNE     NOTJ
ISJ     LDX     PB2
ISJ1    STX     PC1
        BRA     EXEC
ISXD    LDX     SP
        LDA A   5,X
        ADD A   PB2
        STA A   PC2
        LDA A   4,X
        ADC A   #0
        STA A   PC1
        BRA     EXEC
NOTJ    LDX     SP
        CMP A   #$39
        BNE     NOTRTS
NOTJ1   LDX     8,X
        BRA     EXR
NOTRTS  CMP A   #$3B
        BNE     NOTRTI
        LDX     13,X
EXR     STX     PC1
NOTRTI  CMP A   #$3F
        BEQ     NONO
EXEC    LDX     PC1
        LDA A   0,X
        STA A   OPSAVE
        STA B   0,X
        CMP B   0,X
        BNE     CHKROM
CONTG   LDS     SP
        RTI
NONO    JMP     LDABRT
CHKROM  LDA A   PC1
        CMP A   #$C0
        BCS     NONO
        LDX     SP
        LDA A   PB1
        CMP A   #$7E
        BEQ     NOTJ1
        CMP A   #$BD
        BNE     NONO
        LDX     6,X
        INX
        INX
        INX
        BRA     ISJ1
*
TBLKUP  CMP A   #$40
        BCC     TLU6
TLU1    JSR     PNT3C
        LDA A   PB1
        CMP A   #$32
        BEQ     TLU3
        CMP A   #$36
        BEQ     TLU3
        CMP A   #$33
        BEQ     TLU4
        CMP A   #$37
        BEQ     TLU4
TLU2    LDX     #BLANK
        BRA     TLU5
TLU3    LDX     #PNTA
        BRA     TLU5
TLU4    LDX     #PNTB
TLU5    JMP     PDATA1
*
TLU6    CMP A   #$4E
        BEQ     TLU7
        CMP A   #$5E
        BNE     TLU8
TLU7    CLR A
        BRA     TLU1
TLU8    CMP A   #$80
        BCC     TLU9
        AND A   #$4F
        JSR     PNT3C
        LDA A   TEMP
        CMP A   #'*'
        BEQ     TLU2
        LDA A   PB1
        CMP A   #$60
        BCC     TLU2
        AND A   #$10
        BEQ     TLU3
        BRA     TLU4
*
TLU9    AND A   #$3F
        CMP A   #$0F
        BEQ     TLU7
        CMP A   #$07
        BEQ     TLU7
        AND A   #$0F
        CMP A   #$03
        BEQ     TLU7
        CMP A   #$0C
        BGE     TLU10
        ADD A   #$50
        JSR     PNT3C
        LDA A   PB1
        AND A   #$40
        BEQ     TLU3
        BRA     TLU4
*
TLU10   LDA A   PB1
        CMP A   #$8D
        BNE     TLU11
        LDA A   #$53
        BRA     TLU1
TLU11   CMP A   #$C0
        BCC     TLU12
        CMP A   #$9D
        BEQ     TLU7
        AND A   #$0F
        ADD A   #$50
        BRA     TLU13
TLU12   AND A   #$0F
        ADD A   #$52
        CMP A   #$60
        BLT     TLU7
TLU13   JMP     TLU1
*
PNT3C   CLR B
        STA A   TEMP
        ASL A
        ADD A   TEMP
        ADC B   #0
        LDX     #TBL
        STX     XTEMP1
        ADD A   XTEMP1+1
        ADC B   XTEMP1
        STA B   XTEMP1
        STA A   XTEMP1+1
        LDX     XTEMP1
        LDA A   0,X
        STA A   TEMP
        BSR     OUTA
        LDA A   1,X
        BSR     OUTA
        LDA A   2,X
OUTA    JMP     OUTEEE
*
ASCII   JSR     BAD2
        INX
ASCII1  DEX
ASCII2  JSR     INEEE
        CMP A   #8
        BEQ     ASCII1
        STA A   0,X
        CMP A   #4
        BNE     ASCII3
        RTS
ASCII3  INX
        BRA     ASCII2
*
FILL    JSR     LIMITS
        JSR     VALUE
        LDX     BEGA
        DEX
FILL2   INX
        STA A   0,X
        CMP A   0,X
        BNE     FILERR
        CPX     ENDA
        BNE     FILL2
        RTS
FILERR  STX     INXMSB
        STA A   CHKSUM
        JSR     OUTS
        LDX     #INXMSB
        JSR     OUT4HS
        LDX     INXMSB
        JSR     OUT2HS
        LDA A   CHKSUM
        BRA     FILL2
*
LOCATE  JSR     LIMITS
        JSR     VALUE
        TAB
        LDX     BEGA
        DEX
LOC1    INX
        LDA A   0,X
        CBA
        BNE     LOC2
        STX     INXMSB
        JSR     PCRLF
        LDX     #INXMSB
        JSR     OUT4HS
        LDX     INXMSB
LOC2    CPX     ENDA
        BNE     LOC1
        RTS
*
*
* COMPACTED MNEMONIC TBL
*
TBL     FCC     '***NOP'
        FCC     'NOP***'
        FCC     '******'
        FCC     'TAPTPA'
        FCC     'INXDEX'
        FCC     'CLVSEV'
        FCC     'CLCSEC'
        FCC     'CLISEI'
        FCC     'SBACBA'
        FCC     '******'
        FCC     '******'
        FCC     'TABTBA'
        FCC     '***DAA'
        FCC     '***ABA'
        FCC     '******'
        FCC     '******'
        FCC     'BRA***'
        FCC     'BHIBLS'
        FCC     'BCCBCS'
        FCC     'BNEBEQ'
        FCC     'BVCBVS'
        FCC     'BPLBMI'
        FCC     'BGEBLT'
        FCC     'BGTBLE'
        FCC     'TSXINS'
        FCC     'PULPUL'
        FCC     'DESTXS'
        FCC     'PSHPSH'
        FCC     '***RTS'
        FCC     '***RTI'
        FCC     '******'
        FCC     'WAISWI'
        FCC     'NEG***'
        FCC     '***COM'
        FCC     'LSR***'
        FCC     'RORASR'
        FCC     'ASLROL'
        FCC     'DEC***'
        FCC     'INCTST'
        FCC     'JMPCLR'
        FCC     'SUBCMP'
        FCC     'SBCBSR'
        FCC     'ANDBIT'
        FCC     'LDASTA'
        FCC     'EORADC'
        FCC     'ORAADD'
        FCC     'CPXJSR'
        FCC     'LDSSTS'
        FCC     'LDXSTX'
SPLBOO  FCC     '#$'
        FCB     4
COMMX   FCC     ',X'
        FCB     4
BLANK   FCC     '   '
        FCB     4
PNTA    FCC     ' A '
        FCB     4
PNTB    FCC     ' B '
        FCB     4
BREG    FCC     ' B='
        FCB     4
AREG    FCC     'A='
        FCB     4
XREG    FCC     'X='
        FCB     4
SREG    FCC     'S='
        FCB     4
PCTR    FCC     'PC='
        FCB     4
CRLF    FCB     $0D,$0A
        FCB     $15,$04
ADASC   FCC     'BKADDR '
        FCB     4
FROMAD  FCC     'FROM '
        FCC     'ADDR '
        FCB     4
THRUAD  FCC     'THRU '
        FCC     'ADDR '
        FCB     4
TOADD   FCC     'TO ADDR '
        FCB     4
VALASC  FCC     'VALUE '
        FCB     4
*
        ORG ROM+$1FF8
*
IRQTV   FDB     IRQV
SWITV   FDB     SWIV
NMITV   FDB     NMIV
RESTART FDB     START
*
        END     START
