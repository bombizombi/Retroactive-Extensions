//TODO write a description for this script
//@author 
//@category _NEW_
//@keybinding 
//@menupath 
//@toolbar 

import ghidra.app.script.GhidraScript;

import ghidra.program.model.mem.*;
import ghidra.program.model.lang.*;
import ghidra.program.model.pcode.*;
import ghidra.program.model.util.*;
import ghidra.program.model.reloc.*;
import ghidra.program.model.data.*;
import ghidra.program.model.block.*;
import ghidra.program.model.symbol.*;
import ghidra.program.model.scalar.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.address.*;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.List;


public class LoadControl1 extends GhidraScript {

    public void run() throws Exception {

		Address a = askAddress("yo", "enter adr");

		Address adr = currentProgram.getMinAddress();

        Listing list = currentProgram.getListing();
        CodeUnit cu = list.getCodeUnitAt( a);
        cu.setComment( CodeUnit.EOL_COMMENT    , "Sriptup, this is th COMMENT!");

        println("radi");

	AddressFactory f = currentProgram.getAddressFactory();
	Address dada = f.getAddress( "8005");
	println( "dada   " + dada.toString());

        CodeUnit cu3 = list.getCodeUnitAt( dada);

	cu3.setComment( CodeUnit.EOL_COMMENT    , "dadadaad dadada");


	println("axxxxxxxxxxxxxx");

		try {
			List<String> allLines = Files.readAllLines(Paths.get("G:\\cs\\Spectrum_emulator\\GreatEscape\\GreatEscape\\bin\\Debug\\net6.0-windows\\ghidra.ctl"));

			for (String line : allLines) {
				//parse	
				String[] parts = line.split(" ");
				String block = parts[0];
				String len = parts[1];
				//String rest = parts[2]
				String rest =  strJoinRest( parts, "-", 2);
				
				println(block + " " + parseHex(len) + " " + rest);


				Address a2 = adr.getNewAddress( parseHex( len));
			        CodeUnit cu2 = list.getCodeUnitAt( a2);
				println( len);
				if( cu2 != null){
					cu2.setComment( CodeUnit.EOL_COMMENT    , rest);
				}
			}

		} catch (IOException e) {
			println( e.toString());
			//e.printStackTrace();
		}
    }

	public static String strJoinRest(String[] aArr, String sSep, int startingPos) {
		StringBuilder sbStr = new StringBuilder();
		for (int i = startingPos, il = aArr.length; i < il; i++) {
			if (i > startingPos)
				sbStr.append(sSep);
			sbStr.append(aArr[i]);
		}
		return sbStr.toString();
	}
	public static int parseHex( String hex) {
		//check for empty String
		if (hex.isEmpty()) return 0;

		//discard everything after ","

		


		/*
		int pos = hex.indexOf(",");
		if (pos > 0) hex = hex.substring(0, pos);
		*/


		 //check for $ prefix
		if (hex.startsWith("$")) {
			hex = hex.substring(1);
			return Integer.parseInt(hex, 16);
		}
		//else parse decimal	
		return Integer.parseInt(hex, 10);
	}
	


}


























