using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Xamarin.Forms;

namespace ichiglyph.xamarin {
	public partial class MainPage : ContentPage {
		enum Instruction {
			INST_DP_INC,
			INST_DP_DEC,
			INST_VAL_INC,
			INST_VAL_DEC,
			INST_VAL_OUTPUT,
			INST_VAL_ACCEPT,
			INST_JMP_FORWARD,
			INST_JMP_BACK,
			INST_NOP
		};

		private const int DATA_GRANULARITY = 32768;
		private int[] data;
		private int size;

		public MainPage()
		{
			InitializeComponent();
		}

		private void OnFetchClick(object sender, EventArgs e)
		{
			HttpClient client = new HttpClient();
			string url;

			if (picker.SelectedIndex == 0)
				url = "https://raw.githubusercontent.com/martin-decky/ichiglyph/master/examples/hello.ig";
			else
				url = "https://raw.githubusercontent.com/martin-decky/ichiglyph/master/examples/hello.bf";

			source.Text = client.GetStringAsync(url).Result;
		}

		private void OnRunClick(object sender, EventArgs e)
		{
			output.Text = "";
			Run(source.Text);
		}

		private int ProgramSize(string code)
		{
			if (picker.SelectedIndex == 0)
				return code.Length / 2;
			else
				return code.Length;
		}

		private Instruction InstructionDecode(string code, int ip)
		{
			if (picker.SelectedIndex == 0) {
				switch (code[2 * ip]) {
				case 'l':
					switch (code[2 * ip + 1]) {
					case 'l':
						return Instruction.INST_DP_INC;
					case 'I':
						return Instruction.INST_DP_DEC;
					case '1':
						return Instruction.INST_JMP_FORWARD;
					default:
						return Instruction.INST_NOP;
					}
				case 'I':
					switch (code[2 * ip + 1]) {
					case 'l':
						return Instruction.INST_VAL_INC;
					case 'I':
						return Instruction.INST_VAL_DEC;
					case '1':
						return Instruction.INST_JMP_BACK;
					default:
						return Instruction.INST_NOP;
					}
				case '1':
					switch (code[2 * ip + 1]) {
					case 'l':
						return Instruction.INST_VAL_OUTPUT;
					case 'I':
						return Instruction.INST_VAL_ACCEPT;
					default:
						return Instruction.INST_NOP;
					}
				default:
					return Instruction.INST_NOP;
				}
			} else {
				switch (code[ip]) {
				case '>':
					return Instruction.INST_DP_INC;
				case '<':
					return Instruction.INST_DP_DEC;
				case '+':
					return Instruction.INST_VAL_INC;
				case '-':
					return Instruction.INST_VAL_DEC;
				case '.':
					return Instruction.INST_VAL_OUTPUT;
				case ',':
					return Instruction.INST_VAL_ACCEPT;
				case '[':
					return Instruction.INST_JMP_FORWARD;
				case ']':
					return Instruction.INST_JMP_BACK;
				default:
					return Instruction.INST_NOP;
				}
			}
		}

		private void DataInit()
		{
			data = new int[0];
			size = 0;
		}

		private void DataBound(int index)
		{
			if (index >= size) {
				size = index + 1 + DATA_GRANULARITY;
				Array.Resize(ref data, size);
			}
		}

		private void DataInc(int dp)
		{
			DataBound(dp);
			data[dp]++;
		}

		private void DataDec(int dp)
		{
			DataBound(dp);
			data[dp]--;
		}

		private int DataGet(int dp)
		{
			if (dp >= size)
				return 0;

			return data[dp];
		}

		private void DataSet(int dp, int val)
		{
			DataBound(dp);
			data[dp] = val;
		}

		private void Run(string source)
		{
			int program_size = ProgramSize(source);
			int ip = 0;
			int dp = 0;

			DataInit();

			while (ip < program_size) {
				Instruction instruction = InstructionDecode(source, ip);

				/*
				 * Instruction execute.
				 */
				switch (instruction) {
				case Instruction.INST_DP_INC:
					dp++;
					break;
				case Instruction.INST_DP_DEC:
					dp--;
					break;
				case Instruction.INST_VAL_INC:
					DataInc(dp);
					break;
				case Instruction.INST_VAL_DEC:
					DataDec(dp);
					break;
				case Instruction.INST_VAL_OUTPUT:
					int val = DataGet(dp);
					output.Text += Convert.ToChar(val);
					break;
				case Instruction.INST_VAL_ACCEPT:
					int input_val = 0;  // TODO
					DataSet(dp, input_val);
					break;
				case Instruction.INST_JMP_FORWARD:
					val = DataGet(dp);

					if (val == 0) {
						int balance = 1;

						while (balance != 0) {
							if (ip == program_size) {
								/*
								 * No matching instruction can be found.
								 * We simply terminate the execution.
								 */
								break;
							}

							ip++;
							Instruction instruction2 = InstructionDecode(source, ip);

							switch (instruction2) {
							case Instruction.INST_JMP_FORWARD:
								balance++;
								break;
							case Instruction.INST_JMP_BACK:
								balance--;
								break;
							default:
								break;
							}
						}
					}

					break;
				case Instruction.INST_JMP_BACK:
					val = DataGet(dp);

					if (val != 0) {
						int balance = 1;

						while (balance != 0) {
							if (ip == 0) {
								/*
								 * No matching instruction can be found.
								 * We simply terminate the execution.
								 */
								ip = program_size;
								break;
							}

							ip--;
							Instruction instruction2 = InstructionDecode(source, ip);

							switch (instruction2) {
							case Instruction.INST_JMP_FORWARD:
								balance--;
								break;
							case Instruction.INST_JMP_BACK:
								balance++;
								break;
							default:
								break;
							}
						}
					}

					break;
				case Instruction.INST_NOP:
					break;
				}

				ip++;
			}
		}
	}
}
