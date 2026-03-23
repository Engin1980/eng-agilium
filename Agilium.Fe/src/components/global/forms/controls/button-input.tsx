export interface ButtonInputProps {
  label: string;
  className?: string;
  color?: 'red' | 'green' | 'orange' | 'violet' | 'white';
  onClick: () => void;
}

export function ButtonInput(props: ButtonInputProps) {
  let style: string;
  if (props.color == 'red') {
    style = `inline-flex items-center rounded border border-red-600 bg-red-200 text-red-800 hover:bg-red-600 hover:text-white px-4 py-2 text-sm ${props.className || ''}`;
  } else if (props.color == 'green') {
    style = `inline-flex items-center rounded border border-green-600 bg-green-200 text-green-800 hover:bg-green-600 hover:text-white px-4 py-2 text-sm ${props.className || ''}`;
  } else if (props.color == 'orange') {
    style = `inline-flex items-center rounded border border-orange-600 bg-orange-200 text-orange-800 hover:bg-orange-600 hover:text-white px-4 py-2 text-sm ${props.className || ''}`;
  } else if (props.color == 'white') {
    style = `inline-flex items-center rounded border border-gray-400 bg-white text-gray-800 hover:bg-gray-100 hover:text-gray-900 px-4 py-2 text-sm ${props.className || ''}`;
  } else {
    style = `inline-flex items-center rounded border border-violet-600 bg-violet-200 text-violet-800 hover:bg-violet-600 hover:text-white px-4 py-2 text-sm ${props.className || ''}`;
  }
  return (
    <button type="button" className={style} onClick={() => props.onClick()}>
      {props.label}
    </button>
  );
}

export interface SubmitInputProps {
  label: string;
  className?: string;
  color?: 'red' | 'green' | 'orange' | 'violet' | 'white';
}

export function SubmitInput(props: SubmitInputProps) {
  let style: string;
  if (props.color == 'red') {
    style = `w-full py-2 px-4 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 ${props.className || ''}`;
  } else if (props.color == 'green') {
    style = `w-full py-2 px-4 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 ${props.className || ''}`;
  } else if (props.color == 'orange') {
    style = `w-full py-2 px-4 bg-orange-600 text-white rounded-md hover:bg-orange-700 focus:outline-none focus:ring-2 focus:ring-orange-500 ${props.className || ''}`;
  } else if (props.color == 'white') {
    style = `w-full py-2 px-4 bg-white text-gray-800 border border-gray-400 rounded-md hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-gray-500 ${props.className || ''}`;
  } else {
    style = `w-full py-2 px-4 bg-violet-600 text-white rounded-md hover:bg-violet-700 focus:outline-none focus:ring-2 focus:ring-violet-500 ${props.className || ''}`;
  }
  return (
    <button type="submit" className={style}>
      {props.label}
    </button>
  );
}